# 4. Hardware Storage Internals

Understanding the hardware-level behavior of storage systems is crucial for making informed architectural decisions. This section examines the internals of PostgreSQL storage and object storage systems, focusing on how they interact with underlying hardware.

## 4.1 PostgreSQL Storage Architecture

### 4.1.1 Page-Based Storage

PostgreSQL stores data in fixed-size pages (typically 8KB). Each page contains multiple rows, and the database engine manages these pages through a buffer pool in memory. When storing binary data directly in tables, several mechanisms come into play:

**Page Structure:**
- **Header**: Metadata about the page (checksum, free space, etc.)
- **Row Pointers**: Array of pointers to row data
- **Row Data**: Actual table data, including binary columns
- **Free Space**: Unused space within the page

For binary data stored in `BYTEA` columns, the data is stored inline within the page if it fits. However, this can lead to:

- **Page Fragmentation**: Large binary values can cause significant wasted space
- **Row Size Limits**: PostgreSQL has practical limits on row size (typically ~1.6GB, but performance degrades much earlier)
- **Cache Inefficiency**: Mixing small metadata and large binary data in the same pages reduces cache hit rates

### 4.1.2 TOAST (The Oversized-Attribute Storage Technique)

PostgreSQL automatically uses TOAST for values exceeding a threshold (typically 2KB). TOAST moves large values to separate storage:

**TOAST Mechanism:**
1. **Inline Storage**: Small values stored directly in the main table
2. **Extended Storage**: Large values moved to TOAST tables
3. **External Storage**: Very large values stored out-of-line with compression
4. **Main Table Reference**: Main table stores a pointer to TOAST data

**TOAST Implications:**
- **Additional IO**: Reading large values requires additional page reads from TOAST tables
- **Write Amplification**: Updates to large values may require rewriting both main and TOAST pages
- **Compression**: TOAST can compress data, reducing storage but adding CPU overhead
- **Transaction Overhead**: TOAST operations are still transactional, requiring WAL writes

### 4.1.3 Write-Ahead Logging (WAL)

PostgreSQL uses WAL to ensure durability and enable replication:

**WAL Process:**
1. **Write Request**: Application writes data
2. **Buffer Update**: Data written to shared buffer pool
3. **WAL Write**: Change logged to WAL before commit
4. **Checkpoint**: Periodic flushing of dirty pages to disk
5. **WAL Archival**: Old WAL segments archived for point-in-time recovery

**WAL Impact on Binary Data:**
- **Full Value Logging**: Large binary values are fully logged to WAL (unless using `UNLOGGED` tables, which sacrifice durability)
- **Write Amplification**: Each update to a large binary value writes the entire value to WAL
- **Replication Overhead**: WAL-based replication streams all binary data changes
- **Backup Size**: WAL archives contain complete history of binary data changes

### 4.1.4 Buffer Pool and Caching

PostgreSQL's shared buffer pool caches frequently accessed pages:

**Cache Behavior:**
- **Page-Level Caching**: Entire pages cached, not individual rows
- **LRU Eviction**: Least recently used pages evicted when cache is full
- **Mixed Workload Impact**: Binary data can evict metadata pages, reducing cache efficiency
- **Memory Pressure**: Large binary values consume significant buffer pool space

## 4.2 Object Storage Architecture

### 4.2.1 Blob Storage Design

Azure Blob Storage (and Azurite emulation) stores objects as independent entities:

**Object Storage Characteristics:**
- **Flat Namespace**: Objects identified by container and blob name
- **No Transactional Overhead**: Each object write is independent
- **Append-Optimized**: Designed for sequential writes
- **Metadata Separation**: Object metadata stored separately from data

### 4.2.2 Write Patterns

Object storage systems are optimized for different write patterns:

**Write Operations:**
- **Put Blob**: Complete object replacement (atomic)
- **Append Block**: Adding data to existing blob (for append blobs)
- **Block Upload**: Multipart uploads for large files
- **No In-Place Updates**: Objects are immutable; updates create new versions

**Hardware Implications:**
- **Sequential Writes**: Better aligned with SSD write patterns
- **Reduced Write Amplification**: No page-level fragmentation or WAL overhead
- **Parallel Writes**: Multiple objects can be written concurrently without coordination

### 4.2.3 Read Patterns

Object storage read operations:

**Read Characteristics:**
- **Range Reads**: Can read specific byte ranges without fetching entire object
- **Parallel Reads**: Multiple objects can be read concurrently
- **CDN Integration**: Objects can be served via CDN for global distribution
- **No Cache Coherency**: Simpler caching model (no transactional consistency requirements)

## 4.3 Hardware-Level Considerations

### 4.3.1 SSD Write Amplification

Modern SSDs use flash memory with specific characteristics:

**SSD Behavior:**
- **Page Size**: Typically 4KB-16KB pages
- **Block Erasure**: Must erase entire blocks (typically 256KB-2MB) before writing
- **Write Amplification**: Actual writes exceed logical writes due to garbage collection
- **Wear Leveling**: Distributes writes across cells to prevent premature wear

**Impact on Storage Strategies:**
- **Database Storage**: Random writes to pages increase write amplification
- **Object Storage**: Sequential writes better aligned with SSD characteristics
- **Large Files**: Object storage's append-optimized design reduces write amplification

### 4.3.2 IO Patterns

Different storage strategies produce different IO patterns:

**Database Storage IO:**
- **Random Reads/Writes**: Accessing specific pages in tables
- **Mixed Workload**: Metadata and binary data interleaved
- **Small IO Operations**: Many small reads/writes for page management
- **Synchronous Writes**: WAL requires synchronous writes for durability

**Object Storage IO:**
- **Sequential Writes**: Appending data to objects
- **Large IO Operations**: Reading/writing entire objects or large ranges
- **Asynchronous Writes**: Can batch and optimize writes
- **Parallel IO**: Multiple objects accessed concurrently

### 4.3.3 Cache Behavior

CPU and memory caches have different characteristics:

**Database Storage Caching:**
- **Page Cache**: Operating system caches database pages
- **Buffer Pool**: PostgreSQL's own caching layer
- **Cache Pollution**: Large binary values reduce effective cache size for metadata
- **Locality**: Related data (metadata + binary) may be co-located

**Object Storage Caching:**
- **Object Cache**: Can cache entire objects or ranges
- **CDN Cache**: Objects can be cached at edge locations
- **Cache Efficiency**: Metadata queries don't affect object cache
- **Predictable Patterns**: Easier to optimize caching strategies

## 4.4 Performance Implications

The hardware-level differences translate to performance characteristics:

**For Small Files (<100KB):**
- Database storage may be faster due to reduced network overhead
- Cache locality benefits from co-located metadata and data
- Transactional overhead is minimal for small values

**For Large Files (>1MB):**
- Object storage benefits from sequential write patterns
- Reduced write amplification on SSDs
- Better parallelization opportunities
- Lower WAL overhead

**For Mixed Workloads:**
- Database storage can cause cache pollution
- Object storage allows independent scaling of metadata and data access

## 4.5 Summary

Understanding hardware internals reveals why different storage strategies perform differently:

- **PostgreSQL**: Optimized for transactional workloads with small to medium rows; large binary data causes write amplification and cache inefficiency
- **Object Storage**: Optimized for large, immutable objects with sequential access patterns; better aligned with modern SSD characteristics

These fundamental differences form the basis for performance expectations and guide experimental design.

