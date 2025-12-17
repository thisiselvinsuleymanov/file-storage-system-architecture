# Comprehensive Benchmark Results Summary

**Date:** December 17, 2025  
**Test Duration:** ~2 minutes  
**Total Tests:** 360  
**Success Rate:** 100%

## Test Configuration

- **File Sizes:** 1KB, 10KB, 100KB, 1MB, 5MB
- **Storage Types:** Database (PostgreSQL BYTEA), Object Storage (Azurite)
- **Concurrency Levels:** 1, 5, 10, 20 concurrent requests
- **Operations Tested:** Upload, Download, Metadata retrieval

## Key Findings

### Upload Performance

**Database Storage (PostgreSQL):**
- Small files (1-10KB): 0.06-1.22 MB/s
- Medium files (100KB): 1.62-11.48 MB/s
- Large files (1MB): 14.28-76.92 MB/s
- Very large files (5MB): 41.66-136.98 MB/s

**Object Storage (Azurite):**
- Small files (1-10KB): 0.04-0.72 MB/s
- Medium files (100KB): 1.22-6.97 MB/s
- Large files (1MB): 10.00-48.78 MB/s
- Very large files (5MB): 38.46-106.38 MB/s

### Performance Observations

1. **Database storage shows better performance** for small to medium files (<1MB)
2. **Object storage catches up** for larger files (>1MB) but still slightly slower
3. **Concurrency improves throughput** for both storage types, with database storage scaling better
4. **Database storage achieves higher peak throughput** (136.98 MB/s vs 106.38 MB/s)

### Download Performance

Both storage types show excellent download performance:
- Database: 0.09-2500 MB/s (depending on file size and concurrency)
- Object Storage: 0.09-2500 MB/s (similar performance)

### Metadata Performance

- Database storage: 13-334ms average
- Object storage: 20-439ms average
- Database storage is faster for metadata operations

## Detailed Results

See `data/raw/comprehensive-benchmark-20251217-165558.json` for complete results including:
- Individual request latencies
- Throughput calculations
- Concurrency impact analysis
- Statistical breakdowns

## System Metrics

Additional system metrics collected:
- Database size: See `data/raw/db-size-*.csv`
- Database statistics: See `data/raw/db-stats-*.csv`
- Docker container stats: See `data/raw/docker-stats-*.json`

