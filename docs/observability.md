# Observability

The application integrates a comprehensive observability stack using **Grafana** and **OpenTelemetry**.

## Components

- **OpenTelemetry (OTel)**: Used for collecting traces, metrics, and logs from the application.
- **Grafana**: Visualization dashboard for monitoring application health and performance.
- **Loki**: Log aggregation system (configured in `config/loki-config.yaml`).
- **Prometheus**: Monitoring system and time series database (configured in `config/prometheus.yml`).
- **OTel Collector**: Receives telemetry data and exports it to the respective backends (configured in `config/otel-collector-config.yaml`).

## Configuration

The observability stack is spun up via Docker Compose. Check `compose.yaml` and the `config/` directory for details.
