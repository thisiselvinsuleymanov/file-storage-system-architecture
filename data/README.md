# Research Data

This directory contains the collected metrics and benchmark results from the file storage system architecture research.

## Directory Structure

- `raw/` - Raw benchmark results and system metrics
- `processed/` - Processed summaries and analysis

## Key Files

### Benchmark Results
- **`raw/comprehensive-benchmark-20251217-165558.json`** - Complete benchmark results
  - Tests both database (PostgreSQL BYTEA) and object storage (Azurite)
  - File sizes: 1KB, 10KB, 100KB, 1MB, 5MB
  - Concurrency levels: 1, 5, 10, 20 concurrent requests
  - Operations: Upload, Download, Metadata retrieval
  - Total: 360 tests with 100% success rate

### System Metrics
- **`raw/db-stats-*.csv`** - Database file count and size statistics
- **`raw/db-size-*.csv`** - Database and table size information
- **`raw/docker-stats-*.json`** - Container resource usage metrics

### Analysis
- **`processed/benchmark-summary.md`** - Summary of key findings and performance observations

## Usage

All metrics were collected using scripts in the `scripts/` directory:
- `comprehensive-benchmark.sh` - Runs comprehensive API performance benchmarks
- `collect-all-metrics.sh` - Collects system and database metrics

## Reproducibility

The results in this directory were collected using the scripts in `scripts/`. To reproduce:
1. Start services: `cd docker && docker-compose up -d`
2. Wait for services to be healthy
3. Run benchmarks: `./scripts/comprehensive-benchmark.sh`
4. Collect metrics: `./scripts/collect-all-metrics.sh`

## Notes

- All timestamps are in format: YYYYMMDD-HHMMSS
- API accessible at `http://localhost:5001`
- PostgreSQL accessible at `localhost:5432`
- Azurite (object storage) accessible at `localhost:10000`
