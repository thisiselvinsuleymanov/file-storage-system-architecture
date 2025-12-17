# Abstract

Modern web applications face a fundamental architectural decision when handling file storage: should binary data be stored directly in relational databases or delegated to specialized object storage systems? This research presents a comprehensive investigation of file storage strategies in ASP.NET applications, comparing PostgreSQL-based storage against Azure Blob Storage (emulated via Azurite) for large binary objects.

Through systematic benchmarking and hardware-level analysis, we examine performance characteristics including upload throughput, read latency, backup efficiency, and resource utilization. Our experiments reveal significant trade-offs between database storage and object storage, with implications for write amplification, cache behavior, and operational complexity.

The study provides quantitative evidence for storage decision-making, considering factors such as file size distribution, access patterns, and hardware constraints. We present a reproducible experimental framework using Docker containers, enabling researchers and practitioners to validate and extend our findings.

Key contributions include: (1) a comparative performance analysis of database vs. object storage, (2) hardware-aware considerations for storage decisions, (3) a complete reproducible research artifact, and (4) real-world deployment guidance for Azerbaijan-specific contexts.

Our results demonstrate that object storage (Azurite/Azure Blob) provides superior performance for large files (>1MB) and high-throughput scenarios, while database storage offers transactional consistency and simpler operational models for smaller files. The choice depends on specific application requirements, scale, and operational constraints.

**Keywords**: File Storage, PostgreSQL, Object Storage, Azure Blob Storage, Performance Benchmarking, System Architecture

