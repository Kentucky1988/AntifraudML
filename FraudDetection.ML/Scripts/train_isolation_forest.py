import sys
import argparse
import pandas as pd
from sklearn.ensemble import IsolationForest
from skl2onnx import to_onnx
from skl2onnx.common.data_types import FloatTensorType


def train(input_csv: str, output_onnx: str, n_estimators: int = 100):
    """
    Тренує Isolation Forest модель та зберігає її у форматі ONNX.
    
    Args:
        input_csv: Шлях до CSV файлу з тренувальними даними
        output_onnx: Шлях для збереження ONNX моделі
        n_estimators: Кількість дерев у лісі
    """
    # 1. Завантаження даних
    try:
        df = pd.read_csv(input_csv)
        print(f"Data loaded. Rows: {len(df)}, Features: {df.shape[1]}")
    except Exception as e:
        print(f"Error loading CSV: {e}", file=sys.stderr)
        sys.exit(1)

    # 2. Тренування Isolation Forest
    clf = IsolationForest(
        n_estimators=n_estimators,
        contamination='auto',
        random_state=42
    )
    clf.fit(df)
    print("Training completed.")

    # 3. Конвертація в ONNX
    initial_type = [('float_input', FloatTensorType([None, df.shape[1]]))]
    onx = to_onnx(
        clf, 
        df.to_numpy().astype('float32'), 
        initial_types=initial_type,
        target_opset={'': 17, 'ai.onnx.ml': 3}
    )

    # 4. Збереження файлу
    with open(output_onnx, "wb") as f:
        f.write(onx.SerializeToString())
    print(f"Model saved to {output_onnx}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Train Isolation Forest and export to ONNX")
    parser.add_argument("--input", required=True, help="Path to input CSV data")
    parser.add_argument("--output", required=True, help="Path to save ONNX model")
    parser.add_argument("--trees", type=int, default=100, help="Number of trees (default: 100)")
    args = parser.parse_args()
    
    train(args.input, args.output, args.trees)
