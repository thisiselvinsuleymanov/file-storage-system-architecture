# 5. System Architecture

This section describes the overall system architecture, including the components, their interactions, and design decisions.

## 5.1 Architecture Overview

The system implements two storage strategies for comparison:

1. **Database Storage Strategy**: Files stored directly in PostgreSQL
2. **Object Storage Strategy**: Files stored in Azurite (Azure Blob Storage emulation), metadata in PostgreSQL

Both strategies are implemented within the same ASP.NET Core application, allowing direct comparison under identical conditions.

## 5.2 System Components

The system consists of three main components:

**ASP.NET Core API:**
- Upload Controller: Handles file upload requests
- Download Controller: Handles file download requests
- Metadata Controller: Manages file metadata operations
- Storage Service Abstraction: Interface for storage operations
  - Database Storage Strategy: Implements database-based storage
  - Object Storage Strategy: Implements object storage-based storage

**PostgreSQL Database:**
- Stores metadata for both strategies
- Stores file data directly for database storage strategy
- Provides transactional consistency

**Azurite (Azure Blob Storage Emulation):**
- Stores binary file data for object storage strategy
- Provides blob storage API
- Emulates Azure Blob Storage behavior

## 5.3 Storage Strategies

### 5.3.1 Database Storage Strategy

**Design:**
- Files stored in PostgreSQL `BYTEA` columns
- Metadata and binary data in the same table
- Single transaction for file upload
- Direct database queries for file retrieval

**Schema:**
```sql
CREATE TABLE files_db (
    id UUID PRIMARY KEY,
    filename VARCHAR(255),
    content_type VARCHAR(100),
    file_size BIGINT,
    uploaded_at TIMESTAMP,
    file_data BYTEA  -- Binary data stored here
);
```

**Advantages:**
- Transactional consistency
- Single system to manage
- ACID guarantees
- Simpler backup (single database dump)

**Disadvantages:**
- Database size grows with files
- WAL overhead for large files
- Cache pollution
- Backup/restore complexity for large datasets

### 5.3.2 Object Storage Strategy

**Design:**
- Files stored in Azurite blob containers
- Metadata stored in PostgreSQL
- Two-phase commit pattern (metadata + blob)
- Blob storage accessed via Azure SDK

**Schema:**
```sql
CREATE TABLE files_blob (
    id UUID PRIMARY KEY,
    filename VARCHAR(255),
    content_type VARCHAR(100),
    file_size BIGINT,
    uploaded_at TIMESTAMP,
    blob_container VARCHAR(100),
    blob_name VARCHAR(500)  -- Reference to blob storage
);
```

**Advantages:**
- Scalable storage (independent of database)
- Optimized for large files
- Reduced database size
- Better parallelization

**Disadvantages:**
- Two systems to manage
- Eventual consistency concerns
- More complex error handling
- Separate backup procedures

## 5.4 API Design

### 5.4.1 Endpoints

**Upload:**
- `POST /api/files/db/upload` - Upload to database storage
- `POST /api/files/blob/upload` - Upload to object storage

**Download:**
- `GET /api/files/db/{id}` - Download from database storage
- `GET /api/files/blob/{id}` - Download from object storage

**Metadata:**
- `GET /api/files/db/{id}/metadata` - Get metadata (database storage)
- `GET /api/files/blob/{id}/metadata` - Get metadata (object storage)
- `GET /api/files/db` - List all files (database storage)
- `GET /api/files/blob` - List all files (object storage)

### 5.4.2 Service Abstraction

The system uses a strategy pattern to abstract storage operations:

```csharp
public interface IFileStorageService
{
    Task<FileMetadata> UploadAsync(Stream fileStream, string filename, string contentType);
    Task<Stream> DownloadAsync(Guid fileId);
    Task<FileMetadata> GetMetadataAsync(Guid fileId);
    Task DeleteAsync(Guid fileId);
}
```

This allows both storage strategies to be tested with identical API interfaces.

## 5.5 Data Flow

### 5.5.1 Upload Flow (Database Storage)

```
1. Client → API: POST /api/files/db/upload
2. API → Database Storage Service: UploadAsync()
3. Service → PostgreSQL: INSERT INTO files_db (..., file_data)
4. PostgreSQL: Store data in BYTEA column (TOAST if large)
5. PostgreSQL: Write to WAL
6. PostgreSQL → Service: Return file ID
7. Service → API: Return FileMetadata
8. API → Client: 201 Created with metadata
```

### 5.5.2 Upload Flow (Object Storage)

```
1. Client → API: POST /api/files/blob/upload
2. API → Object Storage Service: UploadAsync()
3. Service → Azurite: Upload blob to container
4. Azurite → Service: Return blob URL/name
5. Service → PostgreSQL: INSERT INTO files_blob (..., blob_name)
6. PostgreSQL: Store metadata only
7. PostgreSQL → Service: Return file ID
8. Service → API: Return FileMetadata
9. API → Client: 201 Created with metadata
```

### 5.5.3 Download Flow (Database Storage)

```
1. Client → API: GET /api/files/db/{id}
2. API → Database Storage Service: DownloadAsync(id)
3. Service → PostgreSQL: SELECT file_data FROM files_db WHERE id = ?
4. PostgreSQL: Read from table (and TOAST if needed)
5. PostgreSQL → Service: Return byte array
6. Service → API: Return file stream
7. API → Client: 200 OK with file content
```

### 5.5.4 Download Flow (Object Storage)

```
1. Client → API: GET /api/files/blob/{id}
2. API → Object Storage Service: DownloadAsync(id)
3. Service → PostgreSQL: SELECT blob_name FROM files_blob WHERE id = ?
4. PostgreSQL → Service: Return blob reference
5. Service → Azurite: Download blob by name
6. Azurite → Service: Return blob stream
7. Service → API: Return file stream
8. API → Client: 200 OK with file content
```

## 5.6 Error Handling

### 5.6.1 Database Storage Errors

- **Transaction Rollback**: Failed uploads automatically roll back
- **Constraint Violations**: Handled at database level
- **Storage Limits**: Database size limits apply
- **Connection Failures**: Retry logic with exponential backoff

### 5.6.2 Object Storage Errors

- **Partial Failures**: Metadata saved but blob upload fails (requires cleanup)
- **Blob Not Found**: Handle missing blob references
- **Container Errors**: Handle container creation/access issues
- **Network Failures**: Retry logic for blob operations

## 5.7 Deployment Architecture

The system is designed for Docker-based deployment:

```
┌─────────────────────────────────────────────────────┐
│              Docker Compose Network                 │
│                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────┐ │
│  │   ASP.NET    │  │  PostgreSQL  │  │ Azurite  │ │
│  │     API      │  │              │  │          │ │
│  │  (Port 5000) │  │  (Port 5432) │  │(Ports    │ │
│  │              │  │              │  │ 10000-    │ │
│  │              │  │              │  │ 10002)    │ │
│  └──────────────┘  └──────────────┘  └──────────┘ │
│                                                     │
└─────────────────────────────────────────────────────┘
```

All services communicate over Docker's internal network, ensuring reproducible networking conditions.

## 5.8 Design Decisions

### 5.8.1 Why Azurite Instead of Production Azure?

- **Reproducibility**: Local emulation ensures consistent test conditions
- **Cost**: No cloud costs for experimentation
- **Network Independence**: Eliminates network latency as a variable
- **Control**: Full control over storage backend for testing

### 5.8.2 Why Both Strategies in One Application?

- **Fair Comparison**: Identical runtime conditions
- **Code Reuse**: Shared infrastructure and testing code
- **Simplicity**: Single deployment for both strategies

### 5.8.3 Why PostgreSQL for Metadata in Both Cases?

- **Consistency**: Same metadata storage for fair comparison
- **Realistic**: Most applications use databases for metadata
- **Isolation**: Focuses comparison on binary storage, not metadata storage

## 5.9 Scalability Considerations

### 5.9.1 Database Storage Scaling

- **Vertical Scaling**: Increase database server resources
- **Read Replicas**: Scale read operations
- **Partitioning**: Partition tables by file size or date
- **Archival**: Move old files to separate storage

### 5.9.2 Object Storage Scaling

- **Horizontal Scaling**: Azurite/Azure Blob scales automatically
- **CDN Integration**: Serve files via CDN
- **Parallel Access**: Multiple concurrent reads/writes
- **Tiered Storage**: Use different storage tiers for cost optimization

## 5.10 Summary

The architecture provides a fair, reproducible framework for comparing storage strategies. By implementing both approaches in the same application with identical APIs, we ensure that performance differences reflect storage strategy choices rather than implementation variations.

