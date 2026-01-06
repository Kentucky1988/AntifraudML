import sys
import argparse
import pandas as pd
import lightgbm as lgb
import onnxmltools
from onnxmltools.convert.common.data_types import FloatTensorType


def train(input_csv: str, output_onnx: str, num_leaves: int = 31, learning_rate: float = 0.1, 
          n_estimators: int = 100, lambda_l2: float = 0.1, bagging_fraction: float = 0.8, 
          bagging_freq: int = 1):
    """
    Тренує LightGBM класифікатор та зберігає у форматі ONNX.
    Нормалізація не потрібна для дерев'яних моделей.
    
    Args:
        input_csv: CSV з features (f0, f1, ...) та label
        output_onnx: Шлях для збереження ONNX моделі
        num_leaves: Максимальна кількість листків в дереві
        learning_rate: Швидкість навчання
        n_estimators: Кількість ітерацій бустингу
        lambda_l2: L2 регуляризація (запобігає overfitting)
        bagging_fraction: Частка даних для кожного дерева (0.8 = 80%)
        bagging_freq: Частота bagging (1 = кожну ітерацію)
    """
    try:
        df = pd.read_csv(input_csv)
        print(f"Data loaded. Rows: {len(df)}, Columns: {df.shape[1]}")
    except Exception as e:
        print(f"Error loading CSV: {e}", file=sys.stderr)
        sys.exit(1)

    # Розділяємо features і label
    label_col = 'label'
    if label_col not in df.columns:
        print(f"Error: '{label_col}' column not found", file=sys.stderr)
        sys.exit(1)
    
    X = df.drop(columns=[label_col])
    y = df[label_col]
    
    print(f"Features: {X.shape[1]}, Positive samples: {y.sum()}, Negative: {len(y) - y.sum()}")

    # Тренування LightGBM з регуляризацією
    model = lgb.LGBMClassifier(
        num_leaves=num_leaves,
        learning_rate=learning_rate,
        n_estimators=n_estimators,
        reg_lambda=lambda_l2,           # L2 регуляризація
        subsample=bagging_fraction,      # Bagging fraction
        subsample_freq=bagging_freq,     # Bagging frequency
        random_state=42,
        verbose=-1
    )
    model.fit(X, y)
    print(f"Training completed. Parameters: leaves={num_leaves}, lr={learning_rate}, "
          f"iterations={n_estimators}, l2={lambda_l2}, bagging={bagging_fraction}")

    # Конвертація в ONNX
    initial_type = [('float_input', FloatTensorType([None, X.shape[1]]))]
    onnx_model = onnxmltools.convert_lightgbm(
        model, 
        initial_types=initial_type,
        target_opset=9
    )

    # Збереження
    with open(output_onnx, "wb") as f:
        f.write(onnx_model.SerializeToString())
    print(f"Model saved to {output_onnx}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Train LightGBM classifier and export to ONNX")
    parser.add_argument("--input", required=True, help="Path to input CSV data")
    parser.add_argument("--output", required=True, help="Path to save ONNX model")
    parser.add_argument("--leaves", type=int, default=31, help="Number of leaves (default: 31)")
    parser.add_argument("--lr", type=float, default=0.1, help="Learning rate (default: 0.1)")
    parser.add_argument("--iterations", type=int, default=100, help="Number of iterations (default: 100)")
    parser.add_argument("--lambda_l2", type=float, default=0.1, help="L2 regularization (default: 0.1)")
    parser.add_argument("--bagging", type=float, default=0.8, help="Bagging fraction (default: 0.8)")
    parser.add_argument("--bagging_freq", type=int, default=1, help="Bagging frequency (default: 1)")
    args = parser.parse_args()
    
    train(args.input, args.output, args.leaves, args.lr, args.iterations, 
          args.lambda_l2, args.bagging, args.bagging_freq)
