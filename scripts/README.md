# Scripts

This directory contains scripts for running benchmarks and collecting metrics.

## Available Scripts

### Benchmarking

- **`comprehensive-benchmark.sh`** - Main benchmark script
  - Tests both database (PostgreSQL BYTEA) and object storage (Azurite)
  - Multiple file sizes: 1KB, 10KB, 100KB, 1MB, 5MB
  - Multiple concurrency levels: 1, 5, 10, 20 concurrent requests
  - Measures upload, download, and metadata operations
  - Output: `data/raw/comprehensive-benchmark-*.json`

  Usage:
  ```bash
  ./scripts/comprehensive-benchmark.sh
  ```

### Metrics Collection

- **`collect-all-metrics.sh`** - Collects system and database metrics
  - Docker container statistics (CPU, memory, network)
  - PostgreSQL database statistics (file counts, sizes)
  - Database size information
  - System information
  - Output: Various files in `data/raw/`

  Usage:
  ```bash
  ./scripts/collect-all-metrics.sh
  ```

## Prerequisites

- Docker and Docker Compose (for running services)
- curl (for API testing)
- bc (for calculations)
- python3 (for JSON processing)

## Workflow

These scripts were used to generate the published research results. They are provided for reproducibility:

1. Start services:
   ```bash
   cd docker && docker-compose up -d
   ```

2. Run benchmarks:
   ```bash
   ./scripts/comprehensive-benchmark.sh
   ```

3. Collect metrics:
   ```bash
   ./scripts/collect-all-metrics.sh
   ```

