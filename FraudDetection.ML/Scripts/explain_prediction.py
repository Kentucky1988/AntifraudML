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
    Generates human-readable summary of why transaction was flagged.
    """
    if fraud_prob < 0.5:
        return "Transaction appears legitimate."
    
    # Get top positive contributors (pushing towards fraud)
    fraud_factors = [c for c in top_contributors if c["shap_value"] > 0][:3]
    
    if not fraud_factors:
        return "Transaction flagged as fraud but no clear single factor."
    
    # Feature name mapping (from DepositCounter.AllCounterNames order)
    feature_names_map = {
        # Phone counters (0-8)
        "f0": "IP countries (phone, 1d)", "f1": "IP countries (phone, 7d)", "f2": "IP countries (phone, 30d)",
        "f3": "devices (phone, 1d)", "f4": "devices (phone, 7d)", "f5": "devices (phone, 30d)",
        "f6": "cards (phone, 1d)", "f7": "cards (phone, 7d)", "f8": "cards (phone, 30d)",
        # UserId counters (9-85)
        "f9": "failed transactions (1h)", "f10": "failed transactions in row (1h)", "f11": "failed transactions in row (3d)",
        "f12": "cards (user, 1d)", "f13": "cards (user, 7d)", "f14": "cards (user, 14d)", "f15": "cards (user, 30d)",
        "f16": "device fingerprints (1d)", "f17": "device fingerprints (7d)", "f18": "device fingerprints (30d)",
        "f19": "phones (user, 1d)", "f20": "phones (user, 7d)", "f21": "phones (user, 14d)",
        "f22": "low risk declines (1d)", "f23": "medium risk declines (1d)", "f24": "high risk declines (1d)",
        "f25": "IPs (user, 1d)", "f26": "IPs (user, 7d)", "f27": "IPs (user, 14d)",
        "f28": "IP countries (user, 1d)", "f29": "IP countries (user, 7d)", "f30": "IP countries (user, 14d)",
        "f31": "emails (user, 1d)", "f32": "emails (user, 7d)", "f33": "emails (user, 14d)", "f34": "emails (user, 30d)",
        "f35": "wallets (30d)",
        "f36": "avg seconds between tx (10m)", "f37": "avg seconds between tx (30m)", 
        "f38": "avg seconds between tx (1h)", "f39": "avg seconds between tx (1d)",
        "f40": "avg deposit amount EUR (1d)", "f41": "avg deposit amount EUR (7d)",
        "f42": "avg deposit amount EUR (14d)", "f43": "avg deposit amount EUR (30d)", "f44": "avg deposit amount EUR (60d)",
        "f45": "avg success deposit EUR (1d)", "f46": "avg success deposit EUR (7d)",
        "f47": "avg success deposit EUR (14d)", "f48": "avg success deposit EUR (30d)", "f49": "avg success deposit EUR (60d)",
        "f50": "transactions (1h)", "f51": "pending transactions (1h)", "f52": "pending transactions (1d)",
        "f53": "success payments in row (30d)",
        "f54": "same amount tx in row (1d)", "f55": "same amount tx in row (7d)",
        "f56": "same amount tx in row (14d)", "f57": "same amount tx in row (30d)",
        "f58": "success payments (1d)", "f59": "success payments (7d)", 
        "f60": "success payments (14d)", "f61": "success payments (30d)", "f62": "success payments (60d)",
        "f63": "success/fail ratio (1h)", "f64": "success/fail ratio (1d)",
        "f65": "success/fail ratio (7d)", "f66": "success/fail ratio (14d)", "f67": "success/fail ratio (30d)",
        "f68": "deposit/payout % (1d)", "f69": "deposit/payout % (7d)",
        "f70": "deposit/payout % (14d)", "f71": "deposit/payout % (30d)", "f72": "deposit/payout % (60d)",
        "f73": "payment methods (1h)", "f74": "payment methods (1d)", "f75": "payment methods (7d)",
        "f76": "payment methods (14d)", "f77": "payment methods (30d)",
        "f78": "time since last deposit (s)", "f79": "time since last failed deposit (s)", 
        "f80": "time since last success deposit (s)", "f81": "amount deviation ratio",
        "f82": "sum success deposits card (1h)", "f83": "sum success deposits card (1d)",
        "f84": "sum success deposits card (7d)", "f85": "sum success deposits card (14d)",
        "f86": "sum success deposits card (30d)", "f87": "sum success deposits card (60d)",
        "f88": "country mismatch", "f89": "hour of day", "f90": "day of week",
        "f91": "country mismatch count", "f92": "time to first deposit (s)",
        "f93": "time from first success deposit (s)", "f94": "time from registration (s)",
        "f95": "antifraud declines (1h)", "f96": "antifraud declines (1d)", "f97": "antifraud declines (7d)",
        "f98": "antifraud declines (14d)", "f99": "antifraud declines (30d)",
        # Document counters (100-102)
        "f100": "cards (document, 1d)", "f101": "cards (document, 7d)", "f102": "cards (document, 30d)",
        # SessionIp counters (103-130)
        "f103": "billing countries (IP, 1d)", "f104": "billing countries (IP, 7d)", "f105": "billing countries (IP, 30d)",
        "f106": "docs (IP, 1d)", "f107": "docs (IP, 7d)", "f108": "docs (IP, 30d)",
        "f109": "devices (IP, 1d)", "f110": "devices (IP, 7d)", "f111": "devices (IP, 30d)",
        "f112": "fingerprints (IP, 1d)", "f113": "fingerprints (IP, 7d)", "f114": "fingerprints (IP, 30d)",
        "f115": "cards (IP, 1d)", "f116": "cards (IP, 7d)", "f117": "cards (IP, 30d)",
        "f118": "phones (IP, 1d)", "f119": "phones (IP, 7d)", "f120": "phones (IP, 30d)",
        "f121": "emails (IP, 1d)", "f122": "emails (IP, 7d)", "f123": "emails (IP, 30d)",
        "f124": "users (IP, 1d)", "f125": "users (IP, 7d)", "f126": "users (wallet, 30d)",
        "f127": "avg seconds between tx IP (10m)", "f128": "avg seconds between tx IP (30m)",
        "f129": "avg seconds between tx IP (1h)", "f130": "avg seconds between tx IP (1d)",
        # Email counters (131-137)
        "f131": "devices (email, 1d)", "f132": "devices (email, 7d)", "f133": "devices (email, 30d)",
        "f134": "cards (email, 1d)", "f135": "cards (email, 7d)", "f136": "cards (email, 30d)",
        "f137": "similar emails (30d)",
        # Card counters (138-150)
        "f138": "emails (card, 1d)", "f139": "emails (card, 7d)",
        "f140": "IP countries (card, 1d)", "f141": "IP countries (card, 7d)", "f142": "IP countries (card, 30d)",
        "f143": "devices (card, 1d)", "f144": "devices (card, 7d)", "f145": "devices (card, 30d)",
        "f146": "users (card, 1d)", "f147": "users (card, 7d)", "f148": "users (card, 14d)",
        "f149": "success deposits (card, 30d)", "f150": "failed deposits (card, 1d)",
        # UserIP counters (151-156)
        "f151": "users (userIP, 1d)", "f152": "users (userIP, 30d)",
        "f153": "avg seconds between tx userIP (10m)", "f154": "avg seconds between tx userIP (30m)",
        "f155": "avg seconds between tx userIP (1h)", "f156": "avg seconds between tx userIP (1d)",
        # Anomaly score (last feature)
        "f157": "anomaly score"
    }
    
    reasons = []
    for f in fraud_factors:
        feature = f["feature"]
        value = f["value"]
        
        # Get human-readable name
        readable_name = feature_names_map.get(feature, feature)
        
        # Format value based on type
        if "ratio" in readable_name or "%" in readable_name or "anomaly" in readable_name:
            reasons.append(f"{readable_name}: {value:.2f}")
        elif "seconds" in readable_name or "time" in readable_name:
            reasons.append(f"{readable_name}: {value:.0f}s")
        else:
            reasons.append(f"{readable_name}: {int(value)}")
    
    return f"Flagged as fraud ({fraud_prob:.0%} confidence) due to: {'; '.join(reasons)}."


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate TreeSHAP explanation for fraud prediction")
    parser.add_argument("--model", required=True, help="Path to LightGBM model file (.txt)")
    parser.add_argument("--features", required=True, help="Path to CSV with transaction features")
    parser.add_argument("--output", required=True, help="Path to save explanation JSON")
    parser.add_argument("--top", type=int, default=10, help="Number of top features to show (default: 10)")
    args = parser.parse_args()
    
    explain(args.model, args.features, args.output, args.top)
