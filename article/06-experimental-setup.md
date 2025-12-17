# 6. Experimental Setup

This section describes the experimental methodology, including hardware configuration, software stack, test scenarios, and data collection procedures.

## 6.1 Hardware Configuration

### 6.1.1 Test Environment

**Primary Test Machine:**
- **CPU**: System metrics available in `data/raw/docker-stats-*.json`
- **Memory**: System metrics available in `data/raw/docker-stats-*.json`
- **Storage**: Docker volumes on host filesystem (SSD/HDD dependent on host)
- **Network**: Local Docker network (no external network latency)

**Docker Configuration:**
- **PostgreSQL Container**: 2GB memory limit, 4 CPU cores
- **Azurite Container**: 1GB memory limit, 2 CPU cores
- **API Container**: 1GB memory limit, 2 CPU cores

### 6.1.2 Storage Characteristics

Understanding the underlying storage is critical for interpreting results:

- **SSD vs HDD**: Different IO characteristics
- **Write Amplification**: Measured for both strategies
- **Cache Behavior**: Buffer pool and OS cache effects
- **IO Scheduler**: Linux IO scheduler settings

## 6.2 Software Stack

### 6.2.1 Components

- **ASP.NET Core**: 8.0
- **PostgreSQL**: 16.x
- **Azurite**: Latest stable version
- **Docker**: Latest stable version
- **Docker Compose**: Latest stable version
- **.NET SDK**: 8.0

### 6.2.2 Configuration

**PostgreSQL Settings:**
```ini
shared_buffers = 256MB
effective_cache_size = 1GB
maintenance_work_mem = 64MB
checkpoint_completion_target = 0.9
wal_buffers = 16MB
default_statistics_target = 100
random_page_cost = 1.1  # For SSD
effective_io_concurrency = 200  # For SSD
```

**Azurite Settings:**
- Default configuration (emulating Azure Blob Storage)
- Local file system backend
- No replication or redundancy

## 6.3 Test Scenarios

### 6.3.1 File Size Distribution

Tests cover a range of file sizes to understand performance characteristics:

| File Size Category | Size Range | Test Files |
|-------------------|------------|------------|
| Small | < 100 KB | 100 files |
| Medium | 100 KB - 1 MB | 50 files |
| Large | 1 MB - 10 MB | 20 files |
| Very Large | > 10 MB | 10 files |

### 6.3.2 Test Operations

**Upload Tests:**
- Single file uploads (various sizes)
- Batch uploads (multiple files concurrently)
- Sequential uploads (one after another)
- Mixed workload (small and large files)

**Download Tests:**
- Single file downloads
- Range reads (partial file downloads)
- Concurrent downloads
- Sequential downloads

**Metadata Operations:**
- Metadata queries
- List operations
- Search/filter operations

**Backup Tests:**
- Full database backup (database strategy)
- Blob storage backup (object strategy)
- Backup size comparison
- Backup time comparison

### 6.3.3 Workload Patterns

**Write-Heavy Workload:**
- 80% uploads, 20% downloads
- Simulates content creation scenario

**Read-Heavy Workload:**
- 20% uploads, 80% downloads
- Simulates content delivery scenario

**Balanced Workload:**
- 50% uploads, 50% downloads
- Simulates general-purpose application

## 6.4 Benchmark Methodology

### 6.4.1 Warm-up Phase

Before each test run:
1. System idle for 30 seconds
2. Pre-load 10 files of each size category
3. Allow caches to stabilize
4. Clear performance counters

### 6.4.2 Test Execution

**For Each Test:**
1. **Preparation**: Clear previous test data
2. **Execution**: Run test operation(s)
3. **Measurement**: Collect metrics during execution
4. **Cooldown**: Wait 10 seconds between tests
5. **Repetition**: Run each test 5 times for statistical significance

### 6.4.3 Metrics Collection

**Performance Metrics:**
- **Throughput**: Files per second, MB/s
- **Latency**: P50, P95, P99 percentiles
- **Response Time**: End-to-end operation time
- **Error Rate**: Failed operations percentage

**Resource Metrics:**
- **CPU Usage**: Average and peak CPU utilization
- **Memory Usage**: Peak memory consumption
- **Disk IO**: Read/write operations per second
- **Network IO**: Bytes transferred (for object storage)
- **Database Size**: Total database size growth
- **WAL Size**: Write-ahead log size

**Storage Metrics:**
- **Write Amplification**: Actual writes / logical writes
- **Cache Hit Rate**: Buffer pool and OS cache hit rates
- **Fragmentation**: Database and file system fragmentation

## 6.5 Data Collection Tools

### 6.5.1 Application Metrics

- **Custom Instrumentation**: ASP.NET Core metrics middleware
- **Structured Logging**: JSON logs with timestamps
- **Performance Counters**: .NET performance counters

### 6.5.2 System Metrics

- **Docker Stats**: Container resource usage
- **PostgreSQL Statistics**: `pg_stat_*` views
- **Linux Tools**: `iostat`, `vmstat`, `sar`
- **Database Queries**: Size and performance queries

### 6.5.3 Automated Collection

Scripts collect metrics at regular intervals:
- **During Tests**: Metrics every 1 second
- **Between Tests**: Summary statistics
- **Post-Test**: Final measurements and cleanup

## 6.6 Statistical Analysis

### 6.6.1 Descriptive Statistics

For each metric:
- **Mean**: Average value
- **Median**: 50th percentile
- **Standard Deviation**: Variability measure
- **Min/Max**: Range of values
- **Percentiles**: P50, P95, P99

### 6.6.2 Comparative Analysis

**Between Strategies:**
- **T-Tests**: Statistical significance of differences
- **Effect Size**: Practical significance (Cohen's d)
- **Confidence Intervals**: 95% confidence intervals for means

**Across File Sizes:**
- **Regression Analysis**: Performance vs. file size
- **Breakpoint Analysis**: Identify size thresholds where strategies differ

### 6.6.3 Visualization

- **Time Series**: Performance over time
- **Box Plots**: Distribution comparison
- **Scatter Plots**: Relationship between variables
- **Heat Maps**: Multi-dimensional analysis

## 6.7 Reproducibility Measures

### 6.7.1 Environment Isolation

- **Docker Containers**: Isolated runtime environments
- **Version Pinning**: Specific versions of all dependencies
- **Configuration Files**: All settings in version control
- **Seed Data**: Consistent test data generation

### 6.7.2 Deterministic Testing

- **Fixed Random Seeds**: Reproducible test data
- **Timing Controls**: Consistent timing between runs
- **Resource Limits**: Fixed container resource allocations
- **Network Isolation**: No external network dependencies

### 6.7.3 Documentation

- **Complete Setup Instructions**: Step-by-step reproduction guide
- **All Configurations**: Every setting documented
- **Raw Data**: All collected metrics preserved
- **Analysis Scripts**: Reproducible data analysis

## 6.8 Ethical Considerations

### 6.8.1 Data Privacy

- **No Real User Data**: All test data is synthetic
- **No Personal Information**: No PII in test files
- **Local Testing**: All experiments run locally

### 6.8.2 Resource Usage

- **Contained Experiments**: Limited to test environment
- **No External Services**: No impact on production systems
- **Resource Monitoring**: Track and limit resource consumption

## 6.9 Limitations

### 6.9.1 Test Environment Limitations

- **Single Machine**: Not testing distributed scenarios
- **Local Network**: No real network latency
- **Limited Scale**: Testing up to [X] files, [Y] GB total
- **Synthetic Workloads**: May not match all real-world patterns

### 6.9.2 Measurement Limitations

- **Sampling Frequency**: 1-second intervals may miss brief spikes
- **Container Overhead**: Docker adds some measurement overhead
- **Cache Effects**: Results depend on cache state
- **Timing Precision**: Limited by system clock resolution

## 6.10 Summary

The experimental setup is designed for:
- **Fair Comparison**: Identical conditions for both strategies
- **Statistical Rigor**: Multiple runs and proper analysis
- **Reproducibility**: Complete environment in version control
- **Comprehensive Coverage**: Multiple file sizes and workloads

This methodology ensures that results are reliable, reproducible, and meaningful for decision-making.

