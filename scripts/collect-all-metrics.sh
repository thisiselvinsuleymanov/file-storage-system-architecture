#!/bin/bash

# Comprehensive metrics collection script
# Collects system metrics, database stats, and API performance data

set -e

API_URL="${API_URL:-http://localhost:5001}"
OUTPUT_DIR="data/raw"
PROCESSED_DIR="data/processed"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)

mkdir -p "$OUTPUT_DIR" "$PROCESSED_DIR"

echo "=== Collecting System Metrics ==="

# Docker container stats
echo "Collecting Docker stats..."
docker stats --no-stream --format json > "$OUTPUT_DIR/docker-stats-$TIMESTAMP.json" 2>/dev/null || true

# PostgreSQL database statistics
echo "Collecting PostgreSQL statistics..."
docker exec filestorage-postgres psql -U postgres -d filestorage -t -A -F"," -c "
SELECT 
    'database' as storage_type,
    COUNT(*)::text as file_count,
    COALESCE(SUM(file_size)::text, '0') as total_size_bytes,
    COALESCE(AVG(file_size)::text, '0') as avg_size_bytes
FROM files_db
UNION ALL
SELECT 
    'object' as storage_type,
    COUNT(*)::text as file_count,
    COALESCE(SUM(file_size)::text, '0') as total_size_bytes,
    COALESCE(AVG(file_size)::text, '0') as avg_size_bytes
FROM files_blob;
" > "$OUTPUT_DIR/db-stats-$TIMESTAMP.csv" 2>/dev/null || echo "No data" > "$OUTPUT_DIR/db-stats-$TIMESTAMP.csv"

# Database size information
echo "Collecting database size information..."
docker exec filestorage-postgres psql -U postgres -d filestorage -t -A -F"," -c "
SELECT 
    pg_database_size('filestorage')::text as database_size_bytes,
    pg_total_relation_size('files_db')::text as db_storage_size_bytes,
    pg_total_relation_size('files_blob')::text as blob_metadata_size_bytes;
" > "$OUTPUT_DIR/db-size-$TIMESTAMP.csv" 2>/dev/null || echo "0,0,0" > "$OUTPUT_DIR/db-size-$TIMESTAMP.csv"

# API health check
echo "Checking API health..."
curl -s "$API_URL/swagger/index.html" > /dev/null && echo "API is healthy" > "$OUTPUT_DIR/api-health-$TIMESTAMP.txt" || echo "API is not responding" > "$OUTPUT_DIR/api-health-$TIMESTAMP.txt"

# System information
echo "Collecting system information..."
{
    echo "Hostname: $(hostname)"
    echo "OS: $(uname -a)"
    echo "Docker version: $(docker --version)"
    echo "Timestamp: $TIMESTAMP"
} > "$OUTPUT_DIR/system-info-$TIMESTAMP.txt"

echo ""
echo "=== Metrics Collection Complete ==="
echo "Raw data saved to: $OUTPUT_DIR"
echo "Timestamp: $TIMESTAMP"

