"""
TreeSHAP explanation script for LightGBM fraud predictions.
Generates human-readable explanations for why a transaction was flagged as fraud.

Usage:
    python explain_prediction.py --model model.txt --features features.csv --output explanation.json
    
Input:
    - model: LightGBM model file (saved with model.booster_.save_model())
    - features: CSV with single row of features (same format as training)
    
Output:
    - JSON with SHAP values and top contributing features
"""

import sys
import argparse
import json
import pandas as pd
import numpy as np
import lightgbm as lgb
import shap


def explain(model_path: str, features_csv: str, output_json: str, top_n: int = 10):
    """
    Generates TreeSHAP explanation for a fraud prediction.
    
    Args:
        model_path: Path to LightGBM model file (.txt)
        features_csv: CSV with features for the transaction to explain
        output_json: Path to save explanation JSON
        top_n: Number of top contributing features to include
    """
    # Load model
    try:
        model = lgb.Booster(model_file=model_path)
        print(f"Model loaded from {model_path}")
    except Exception as e:
        print(f"Error loading model: {e}", file=sys.stderr)
        sys.exit(1)
    
    # Load features
    try:
        df = pd.read_csv(features_csv)
        if len(df) == 0:
            print("Error: Empty features file", file=sys.stderr)
            sys.exit(1)
        print(f"Features loaded: {df.shape[1]} features")
    except Exception as e:
        print(f"Error loading features: {e}", file=sys.stderr)
        sys.exit(1)
    
    # Get feature names
    feature_names = list(df.columns)
    
    # Create TreeExplainer (optimized for tree models)
    explainer = shap.TreeExplainer(model)
    
    # Calculate SHAP values
    shap_values = explainer.shap_values(df)
    
    # For binary classification, shap_values might be a list [class_0, class_1]
    # We want class_1 (fraud) explanations
    if isinstance(shap_values, list):
        shap_values = shap_values[1]  # Fraud class
    
    # Get values for first (and only) row
    shap_row = shap_values[0] if len(shap_values.shape) > 1 else shap_values
    feature_values = df.iloc[0].values
    
    # Get base value (expected value / average prediction)
    base_value = explainer.expected_value
    if isinstance(base_value, (list, np.ndarray)):
        base_value = base_value[1] if len(base_value) > 1 else base_value[0]
    
    # Create feature contributions list
    contributions = []
    for i, (name, shap_val, feat_val) in enumerate(zip(feature_names, shap_row, feature_values)):
        contributions.append({
            "feature": name,
            "value": float(feat_val),
            "shap_value": float(shap_val),
            "impact": "increases_fraud" if shap_val > 0 else "decreases_fraud"
        })
    
    # Sort by absolute SHAP value (most important first)
    contributions.sort(key=lambda x: abs(x["shap_value"]), reverse=True)
    
    # Get model prediction
    prediction_raw = model.predict(df)[0]
    # Convert log-odds to probability if needed
    prediction_prob = 1 / (1 + np.exp(-prediction_raw)) if prediction_raw < 0 or prediction_raw > 1 else prediction_raw
    
    # Build explanation
    explanation = {
        "transaction_id": df.get("transaction_id", ["unknown"])[0] if "transaction_id" in df.columns else "unknown",
        "prediction": {
            "is_fraud": bool(prediction_prob >= 0.5),
            "fraud_probability": float(prediction_prob),
            "raw_score": float(prediction_raw)
        },
        "base_value": float(base_value),
        "total_shap": float(sum(shap_row)),
        "top_contributors": contributions[:top_n],
        "all_contributions": contributions,
        "summary": generate_summary(contributions[:top_n], prediction_prob)
    }
    
    # Save to JSON
    with open(output_json, 'w', encoding='utf-8') as f:
        json.dump(explanation, f, indent=2, ensure_ascii=False)
    
    print(f"Explanation saved to {output_json}")
    print(f"\nPrediction: {'FRAUD' if prediction_prob >= 0.5 else 'LEGIT'} ({prediction_prob:.2%})")
    print(f"\nTop {top_n} contributing features:")
    for c in contributions[:top_n]:
        sign = "+" if c["shap_value"] > 0 else ""
        print(f"  {sign}{c['shap_value']:.4f}  {c['feature']} = {c['value']:.2f}")


def generate_summary(top_contributors: list, fraud_prob: float) -> str:
    """
    Generates summary of why transaction was flagged.
    Feature names are mapped to human-readable format in C# code.
    """
    if fraud_prob < 0.5:
        return "Transaction appears legitimate."
    
    # Get top positive contributors (pushing towards fraud)
    fraud_factors = [c for c in top_contributors if c["shap_value"] > 0][:3]
    
    if not fraud_factors:
        return "Transaction flagged as fraud but no clear single factor."
    
    # Simple summary with feature indices (C# will map to readable names)
    reasons = [f"{f['feature']} = {f['value']:.2f}" for f in fraud_factors]
    
    return f"Flagged as fraud ({fraud_prob:.0%} confidence). Top factors: {'; '.join(reasons)}."


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate TreeSHAP explanation for fraud prediction")
    parser.add_argument("--model", required=True, help="Path to LightGBM model file (.txt)")
    parser.add_argument("--features", required=True, help="Path to CSV with transaction features")
    parser.add_argument("--output", required=True, help="Path to save explanation JSON")
    parser.add_argument("--top", type=int, default=10, help="Number of top features to show (default: 10)")
    args = parser.parse_args()
    
    explain(args.model, args.features, args.output, args.top)
