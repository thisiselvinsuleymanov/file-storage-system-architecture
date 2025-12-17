-- Database Storage Table
CREATE TABLE IF NOT EXISTS files_db (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    filename VARCHAR(255) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    file_size BIGINT NOT NULL,
    uploaded_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    file_data BYTEA NOT NULL
);

-- Indexes for database storage
CREATE INDEX IF NOT EXISTS idx_files_db_uploaded_at ON files_db(uploaded_at);
CREATE INDEX IF NOT EXISTS idx_files_db_filename ON files_db(filename);

-- Object Storage Metadata Table
CREATE TABLE IF NOT EXISTS files_blob (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    filename VARCHAR(255) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    file_size BIGINT NOT NULL,
    uploaded_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    blob_container VARCHAR(100) NOT NULL,
    blob_name VARCHAR(500) NOT NULL,
    UNIQUE(blob_container, blob_name)
);

-- Indexes for object storage metadata
CREATE INDEX IF NOT EXISTS idx_files_blob_uploaded_at ON files_blob(uploaded_at);
CREATE INDEX IF NOT EXISTS idx_files_blob_filename ON files_blob(filename);
CREATE INDEX IF NOT EXISTS idx_files_blob_container_name ON files_blob(blob_container, blob_name);

-- Statistics and monitoring views
CREATE OR REPLACE VIEW storage_stats AS
SELECT 
    'database' as storage_type,
    COUNT(*) as file_count,
    SUM(file_size) as total_size,
    AVG(file_size) as avg_size,
    MIN(file_size) as min_size,
    MAX(file_size) as max_size
FROM files_db
UNION ALL
SELECT 
    'object' as storage_type,
    COUNT(*) as file_count,
    SUM(file_size) as total_size,
    AVG(file_size) as avg_size,
    MIN(file_size) as min_size,
    MAX(file_size) as max_size
FROM files_blob;

