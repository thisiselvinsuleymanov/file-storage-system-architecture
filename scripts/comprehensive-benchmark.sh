#!/bin/bash

# Comprehensive benchmark script
# Tests both database and object storage with various file sizes and concurrency levels

set -e

API_URL="${API_URL:-http://localhost:5001}"
OUTPUT_DIR="data/raw"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
RESULTS_FILE="$OUTPUT_DIR/comprehensive-benchmark-$TIMESTAMP.json"

mkdir -p "$OUTPUT_DIR"

echo "=== Comprehensive Storage Benchmark ==="
echo "API URL: $API_URL"
echo "Timestamp: $TIMESTAMP"
echo ""

# Test file sizes: 1KB, 10KB, 100KB, 1MB, 5MB (staying under 30MB limit)
SIZES=(1024 10240 102400 1048576 5242880)
STORAGE_TYPES=("db" "blob")
CONCURRENCY_LEVELS=(1 5 10 20)

# Initialize results
RESULTS="{"
RESULTS+="\"timestamp\":\"$TIMESTAMP\","
RESULTS+="\"api_url\":\"$API_URL\","
RESULTS+="\"test_config\":{"
RESULTS+="\"file_sizes_bytes\":[1024,10240,102400,1048576,5242880],"
RESULTS+="\"storage_types\":[\"db\",\"blob\"],"
RESULTS+="\"concurrency_levels\":[1,5,10,20]"
RESULTS+="},"
RESULTS+="\"results\":["

TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Function to extract file ID from JSON response
extract_file_id() {
    local response=$1
    echo "$response" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('id', ''))" 2>/dev/null || echo ""
}

# Run benchmarks for each storage type
for storage in "${STORAGE_TYPES[@]}"; do
    echo "=== Testing $storage storage ==="
    
    for size in "${SIZES[@]}"; do
        echo "  File size: $size bytes ($(echo "scale=2; $size/1024" | bc) KB)"
        
        # Create test file
        TEST_FILE="/tmp/benchmark-test-$size-$$.bin"
        head -c $size < /dev/urandom > "$TEST_FILE"
        
        # Test with different concurrency levels
        for concurrency in "${CONCURRENCY_LEVELS[@]}"; do
            echo "    Concurrency: $concurrency"
            
            # Arrays to store results
            declare -a upload_times=()
            declare -a download_times=()
            declare -a metadata_times=()
            declare -a file_ids=()
            
            # Upload phase with concurrency
            upload_start=$(date +%s%N)
            upload_pids=()
            
            for ((i=1; i<=concurrency; i++)); do
                (
                    start=$(date +%s%N)
                    response=$(curl -s -X POST \
                        -F "file=@$TEST_FILE" \
                        -F "filename=test-$size-$i-$$.bin" \
                        "$API_URL/api/files/$storage/upload" 2>&1)
                    end=$(date +%s%N)
                    duration=$((($end - $start) / 1000000))
                    
                    file_id=$(extract_file_id "$response")
                    if [ -n "$file_id" ] && [ "$file_id" != "null" ]; then
                        echo "$file_id|$duration|success" >> /tmp/upload-$$-$i.txt
                    else
                        echo "|$duration|failed" >> /tmp/upload-$$-$i.txt
                    fi
                ) &
                upload_pids+=($!)
            done
            
            # Wait for all uploads
            for pid in "${upload_pids[@]}"; do
                wait $pid
            done
            
            upload_end=$(date +%s%N)
            total_upload_time=$((($upload_end - $upload_start) / 1000000))
            
            # Collect upload results
            for ((i=1; i<=concurrency; i++)); do
                if [ -f "/tmp/upload-$$-$i.txt" ]; then
                    while IFS='|' read -r file_id duration status; do
                        if [ "$status" = "success" ] && [ -n "$file_id" ]; then
                            file_ids+=("$file_id")
                            upload_times+=("$duration")
                        fi
                    done < "/tmp/upload-$$-$i.txt"
                    rm -f "/tmp/upload-$$-$i.txt"
                fi
            done
            
            # Download phase with concurrency
            if [ ${#file_ids[@]} -gt 0 ]; then
                download_start=$(date +%s%N)
                download_pids=()
                
                for file_id in "${file_ids[@]}"; do
                    (
                        start=$(date +%s%N)
                        curl -s -o /dev/null "$API_URL/api/files/$storage/$file_id/download" > /dev/null 2>&1
                        end=$(date +%s%N)
                        duration=$((($end - $start) / 1000000))
                        echo "$duration" >> /tmp/download-$$.txt
                    ) &
                    download_pids+=($!)
                done
                
                for pid in "${download_pids[@]}"; do
                    wait $pid
                done
                
                download_end=$(date +%s%N)
                total_download_time=$((($download_end - $download_start) / 1000000))
                
                # Collect download results
                if [ -f "/tmp/download-$$.txt" ]; then
                    while read -r duration; do
                        download_times+=("$duration")
                    done < "/tmp/download-$$.txt"
                    rm -f "/tmp/download-$$.txt"
                fi
                
                # Metadata phase
                metadata_start=$(date +%s%N)
                metadata_pids=()
                
                for file_id in "${file_ids[@]}"; do
                    (
                        start=$(date +%s%N)
                        curl -s "$API_URL/api/files/$storage/$file_id" > /dev/null 2>&1
                        end=$(date +%s%N)
                        duration=$((($end - $start) / 1000000))
                        echo "$duration" >> /tmp/metadata-$$.txt
                    ) &
                    metadata_pids+=($!)
                done
                
                for pid in "${metadata_pids[@]}"; do
                    wait $pid
                done
                
                metadata_end=$(date +%s%N)
                total_metadata_time=$((($metadata_end - $metadata_start) / 1000000))
                
                # Collect metadata results
                if [ -f "/tmp/metadata-$$.txt" ]; then
                    while read -r duration; do
                        metadata_times+=("$duration")
                    done < "/tmp/metadata-$$.txt"
                    rm -f "/tmp/metadata-$$.txt"
                fi
                
                # Calculate statistics using awk (more reliable than bc)
                upload_sum=0
                for t in "${upload_times[@]}"; do
                    upload_sum=$((upload_sum + t))
                done
                upload_avg=$((upload_sum / ${#upload_times[@]}))
                
                download_sum=0
                for t in "${download_times[@]}"; do
                    download_sum=$((download_sum + t))
                done
                download_avg=$((download_sum / ${#download_times[@]}))
                
                metadata_sum=0
                for t in "${metadata_times[@]}"; do
                    metadata_sum=$((metadata_sum + t))
                done
                metadata_avg=$((metadata_sum / ${#metadata_times[@]}))
                
                # Calculate throughput (MB/s)
                size_mb=$(echo "scale=6; $size / 1048576" | bc)
                if [ $total_upload_time -gt 0 ]; then
                    upload_throughput=$(echo "scale=2; ($size_mb * ${#upload_times[@]}) / ($total_upload_time / 1000)" | bc)
                else
                    upload_throughput=0
                fi
                
                if [ $total_download_time -gt 0 ]; then
                    download_throughput=$(echo "scale=2; ($size_mb * ${#download_times[@]}) / ($total_download_time / 1000)" | bc)
                else
                    download_throughput=0
                fi
                
                # Add to results
                RESULTS+="{"
                RESULTS+="\"storage\":\"$storage\","
                RESULTS+="\"file_size_bytes\":$size,"
                RESULTS+="\"concurrency\":$concurrency,"
                RESULTS+="\"successful_requests\":${#file_ids[@]},"
                RESULTS+="\"upload\":{"
                RESULTS+="\"total_time_ms\":$total_upload_time,"
                RESULTS+="\"avg_time_ms\":$upload_avg,"
                RESULTS+="\"throughput_mbps\":$upload_throughput,"
                RESULTS+="\"times_ms\":[$(IFS=','; echo "${upload_times[*]}")]"
                RESULTS+="},"
                RESULTS+="\"download\":{"
                RESULTS+="\"total_time_ms\":$total_download_time,"
                RESULTS+="\"avg_time_ms\":$download_avg,"
                RESULTS+="\"throughput_mbps\":$download_throughput,"
                RESULTS+="\"times_ms\":[$(IFS=','; echo "${download_times[*]}")]"
                RESULTS+="},"
                RESULTS+="\"metadata\":{"
                RESULTS+="\"total_time_ms\":$total_metadata_time,"
                RESULTS+="\"avg_time_ms\":$metadata_avg,"
                RESULTS+="\"times_ms\":[$(IFS=','; echo "${metadata_times[*]}")]"
                RESULTS+="}"
                RESULTS+="},"
                
                PASSED_TESTS=$(($PASSED_TESTS + ${#file_ids[@]}))
                
                # Cleanup - delete files
                for file_id in "${file_ids[@]}"; do
                    curl -s -X DELETE "$API_URL/api/files/$storage/$file_id" > /dev/null 2>&1 &
                done
                wait
                
                echo "      ✓ Upload: ${#file_ids[@]}/$concurrency successful, avg: ${upload_avg}ms, throughput: ${upload_throughput} MB/s"
            else
                FAILED_TESTS=$(($FAILED_TESTS + $concurrency))
                echo "      ✗ All uploads failed"
            fi
            
            TOTAL_TESTS=$(($TOTAL_TESTS + $concurrency))
            
            # Small delay between tests
            sleep 1
        done
    done
    
    rm -f "$TEST_FILE"
done

# Close results array
RESULTS="${RESULTS%,}]"
RESULTS+=",\"summary\":{"
RESULTS+="\"total_tests\":$TOTAL_TESTS,"
RESULTS+="\"passed_tests\":$PASSED_TESTS,"
RESULTS+="\"failed_tests\":$FAILED_TESTS,"
RESULTS+="\"success_rate\":$(echo "scale=2; $PASSED_TESTS * 100 / $TOTAL_TESTS" | bc)%"
RESULTS+="}"
RESULTS+="}"

# Save results
echo "$RESULTS" | python3 -m json.tool > "$RESULTS_FILE" 2>/dev/null || echo "$RESULTS" > "$RESULTS_FILE"

echo ""
echo "=== Benchmark Complete ==="
echo "Results saved to: $RESULTS_FILE"
echo "Total tests: $TOTAL_TESTS"
echo "Passed: $PASSED_TESTS"
echo "Failed: $FAILED_TESTS"
echo "Success rate: $(echo "scale=2; $PASSED_TESTS * 100 / $TOTAL_TESTS" | bc)%"
