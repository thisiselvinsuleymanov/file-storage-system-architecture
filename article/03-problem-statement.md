# 3. Problem Statement

## 3.1 Core Research Question

**How do file storage strategies (database storage vs. object storage) compare in terms of performance, resource utilization, and operational characteristics for ASP.NET applications?**

This question encompasses several sub-questions:

1. What are the performance differences (throughput, latency) between storing files in PostgreSQL versus Azure Blob Storage?
2. How do file size and access patterns affect the relative performance of each approach?
3. What are the hardware-level implications (IO operations, write amplification, cache behavior) of each storage strategy?
4. What are the operational trade-offs (backup complexity, scalability, maintenance) between approaches?
5. Under what conditions should each approach be preferred?

## 3.2 Problem Motivation

### 3.2.1 The Storage Decision Dilemma

System architects face a fundamental choice when designing file storage:

**Option A: Database Storage**
- Store files directly in PostgreSQL (using BYTEA or TOAST)
- Metadata and binary data in the same system
- Transactional consistency guarantees
- Simpler operational model

**Option B: Object Storage**
- Store files in Azure Blob Storage (or similar)
- Store only metadata in PostgreSQL
- Separate systems for metadata and data
- More complex operational model

Both approaches have advocates, but decision-making is often based on intuition, vendor recommendations, or limited anecdotal evidence rather than systematic evaluation.

### 3.2.2 Real-World Impact

The storage decision has cascading effects:

- **Performance**: Affects user experience, especially for large file uploads/downloads
- **Cost**: Different resource requirements and scaling characteristics
- **Reliability**: Different failure modes and recovery mechanisms
- **Maintainability**: Different operational procedures and skill requirements
- **Scalability**: Different bottlenecks and scaling strategies

### 3.2.3 Lack of Empirical Evidence

While both approaches are widely used, there is limited published research providing:

- Quantitative performance comparisons under identical conditions
- Hardware-level analysis of resource utilization
- Guidance on when to choose each approach
- Reproducible experimental frameworks

## 3.3 Research Hypotheses

Based on preliminary analysis and related work, we formulate the following hypotheses:

**H1**: Object storage (Azurite/Azure Blob) will demonstrate higher throughput for large files (>1MB) compared to database storage.

**H2**: Database storage will show lower latency for small files (<100KB) due to reduced network overhead and cache locality.

**H3**: Database storage will exhibit higher write amplification due to WAL and page-level updates.

**H4**: Backup operations will be faster for object storage due to incremental and parallel capabilities.

**H5**: Resource utilization (CPU, memory, disk IO) will differ significantly between approaches, with database storage showing higher variability.

## 3.4 Success Criteria

This research will be considered successful if it:

1. Provides quantitative performance data comparing both approaches
2. Identifies clear decision criteria based on file characteristics and use cases
3. Delivers a reproducible experimental framework
4. Offers actionable guidance for system architects
5. Contributes to the body of knowledge on storage system design

## 3.5 Scope Definition

### 3.5.1 In Scope

- Performance benchmarking (throughput, latency)
- Resource utilization analysis (CPU, memory, disk IO)
- Hardware-level considerations (write amplification, cache effects)
- Backup and recovery operations
- ASP.NET Core implementation patterns
- Docker-based reproducible environment

### 3.5.2 Out of Scope

- Cost analysis (though resource utilization data can inform cost estimates)
- Multi-region deployment and replication
- Security and access control mechanisms (assumed equivalent)
- Production Azure Blob Storage (using Azurite for reproducibility)
- Other object storage systems (S3, GCS) beyond Azure Blob
- Long-term durability and archival storage

## 3.6 Expected Contributions

This research contributes:

1. **Empirical Evidence**: Quantitative performance data from systematic benchmarking
2. **Hardware Awareness**: Analysis connecting storage decisions to hardware behavior
3. **Reproducible Artifact**: Complete experimental framework for validation and extension
4. **Practical Guidance**: Decision criteria and recommendations for practitioners
5. **Research Foundation**: Baseline for future studies on storage architectures

