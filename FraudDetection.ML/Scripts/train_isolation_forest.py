import sys
import argparse
import pandas as pd
from sklearn.ensemble import IsolationForest
from sklearn.preprocessing import MinMaxScaler
from sklearn.pipeline import Pipeline
from skl2onnx import to_onnx
from skl2onnx.common.data_types import FloatTensorType


def train(input_csv: str, output_onnx: str, n_estimators: int = 100):
    """
    Тренує Isolation Forest з вбудованою нормалізацією та зберігає у форматі ONNX.
    Pipeline: MinMaxScaler -> IsolationForest
    
    Args:
        input_csv: Шлях до CSV файлу з сирими даними (без нормалізації)
        output_onnx: Шлях для збереження ONNX моделі
        n_estimators: Кількість дерев у лісі
    """
    # 1. Читаємо сирі дані
    try:
        df = pd.read_csv(input_csv)
        print(f"Data loaded. Rows: {len(df)}, Features: {df.shape[1]}")
    except Exception as e:
        print(f"Error loading CSV: {e}", file=sys.stderr)
        sys.exit(1)

    # 2. Створюємо Pipeline: Scaler -> Model
    # Scaler автоматично знайде min/max і збереже їх в ONNX
    pipeline = Pipeline([
        ('scaler', MinMaxScaler()),
        ('model', IsolationForest(
            n_estimators=n_estimators,
            contamination='auto',
            random_state=42
        ))
    ])

    # 3. Тренуємо весь pipeline
    pipeline.fit(df)
    print("Training completed.")

    # 4. Конвертація в ONNX (весь pipeline, включаючи scaler)
    initial_type = [('float_input', FloatTensorType([None, df.shape[1]]))]
    onx = to_onnx(
        pipeline,
        df.to_numpy().astype('float32'),
        initial_types=initial_type,
        target_opset={'': 17, 'ai.onnx.ml': 3}
    )

    # 5. Збереження
    with open(output_onnx, "wb") as f:
        f.write(onx.SerializeToString())
    print(f"Model saved to {output_onnx}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Train Isolation Forest Pipeline and export to ONNX")
    parser.add_argument("--input", required=True, help="Path to input CSV data")
    parser.add_argument("--output", required=True, help="Path to save ONNX model")
    parser.add_argument("--trees", type=int, default=100, help="Number of trees (default: 100)")
    args = parser.parse_args()
    
    train(args.input, args.output, args.trees)
