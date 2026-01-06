# Multi-stage build for .NET + Python (native ARM/x64)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY FraudDetection.sln .
COPY FraudDetection.Core/FraudDetection.Core.csproj FraudDetection.Core/
COPY FraudDetection.ML/FraudDetection.ML.csproj FraudDetection.ML/
COPY FraudDetection.Demo/FraudDetection.Demo.csproj FraudDetection.Demo/
COPY FraudDetection.Tests/FraudDetection.Tests.csproj FraudDetection.Tests/

RUN dotnet restore

COPY FraudDetection.Core/ FraudDetection.Core/
COPY FraudDetection.ML/ FraudDetection.ML/
COPY FraudDetection.Demo/ FraudDetection.Demo/

RUN dotnet publish FraudDetection.Demo/FraudDetection.Demo.csproj -c Release --no-restore -o /app/publish

# Runtime with Python
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime

RUN apt-get update && apt-get install -y --no-install-recommends \
    python3 \
    python3-pip \
    python3-venv \
    libgomp1 \
    && rm -rf /var/lib/apt/lists/*

RUN python3 -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"
RUN pip install --no-cache-dir packaging pandas scikit-learn skl2onnx onnx onnxmltools lightgbm shap

WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/models

ENV PYTHON_PATH="/opt/venv/bin/python3"
ENTRYPOINT ["dotnet", "FraudDetection.Demo.dll"]
