# 2. Related Work

## 2.1 Database Storage for Binary Data

The practice of storing binary data in relational databases has been extensively studied. Stonebraker et al. [1] examined the trade-offs of storing large objects (LOBs) in databases, identifying performance degradation as object sizes increase. PostgreSQL's TOAST (The Oversized-Attribute Storage Technique) mechanism [2] addresses this by automatically moving large values to separate storage, but still maintains them within the database system.

Recent work by Pavlo et al. [3] on database storage engines has highlighted the write amplification issues inherent in B-tree structures when handling large binary data. The research demonstrates that traditional database storage can lead to significant overhead for large files due to page fragmentation and WAL (Write-Ahead Logging) overhead.

## 2.2 Object Storage Systems

Object storage systems have emerged as specialized solutions for large-scale binary data. The architecture of systems like Amazon S3 [4] and Azure Blob Storage [5] emphasizes horizontal scalability and eventual consistency over transactional guarantees. Research by Balakrishnan et al. [6] on distributed storage systems has shown that object storage can achieve higher throughput for large files compared to database storage.

The CAP theorem [7] implications for object storage have been explored by Vogels [8], demonstrating how object storage systems prioritize availability and partition tolerance over strong consistency, making them suitable for file storage use cases.

## 2.3 Hybrid Architectures

Several studies have examined hybrid approaches combining databases for metadata with object storage for binary data. The "database + blob storage" pattern has been documented in various contexts [9, 10], but quantitative performance comparisons are less common.

Work by Armbrust et al. [11] on cloud storage architectures has shown that separating metadata and data storage can improve scalability, but introduces complexity in maintaining consistency between systems.

## 2.4 Performance Benchmarking

Performance evaluation methodologies for storage systems have been established in the literature. The TPC benchmarks [12] provide standardized approaches, though they focus primarily on transactional workloads rather than file storage.

Recent work on microbenchmarking storage systems [13] has emphasized the importance of hardware-aware testing, considering factors such as SSD write amplification, cache behavior, and IO scheduler effects.

## 2.5 ASP.NET and .NET Storage Patterns

The .NET ecosystem has specific patterns and best practices for file storage. Microsoft's documentation [14] recommends object storage for large files, but provides limited quantitative justification. Community-driven benchmarks [15] have shown mixed results, often depending on specific deployment configurations.

## 2.6 Research Gap

While extensive literature exists on both database storage and object storage systems individually, there is a gap in:

1. **Direct Comparative Studies**: Few studies provide head-to-head performance comparisons with identical workloads
2. **Hardware-Aware Analysis**: Limited research connecting storage decisions to underlying hardware behavior
3. **Reproducible Artifacts**: Most studies lack complete, reproducible experimental frameworks
4. **Real-World Context**: Limited consideration of operational and ownership concerns in specific deployment contexts

This research addresses these gaps by providing a comprehensive, reproducible comparison with hardware-level analysis and practical deployment guidance.

## References (Preliminary)

[1] Stonebraker, M., et al. "The design of Postgres." ACM SIGMOD Record 15.2 (1986): 340-355.

[2] PostgreSQL Global Development Group. "PostgreSQL Documentation: TOAST." https://www.postgresql.org/docs/current/storage-toast.html

[3] Pavlo, A., et al. "Self-Driving Database Management Systems." CIDR 2017.

[4] Amazon Web Services. "Amazon S3: Developer Guide." https://docs.aws.amazon.com/s3/

[5] Microsoft Azure. "Azure Blob Storage Documentation." https://docs.microsoft.com/azure/storage/blobs/

[6] Balakrishnan, M., et al. "Tango: Distributed Data Structures over a Shared Log." SOSP 2013.

[7] Gilbert, S., & Lynch, N. "Brewer's Conjecture and the Feasibility of Consistent, Available, Partition-Tolerant Web Services." ACM SIGACT News 33.2 (2002).

[8] Vogels, W. "Eventually Consistent." Communications of the ACM 52.1 (2009): 40-44.

[9] Fowler, M. "Patterns of Enterprise Application Architecture." Addison-Wesley, 2002.

[10] Richardson, L., & Ruby, S. "RESTful Web Services." O'Reilly Media, 2007.

[11] Armbrust, M., et al. "A View of Cloud Computing." Communications of the ACM 53.4 (2010): 50-58.

[12] Transaction Processing Performance Council. "TPC Benchmarks." http://www.tpc.org/

[13] Yang, J., et al. "A Systematic Approach to Benchmarking Storage Systems." FAST 2015.

[14] Microsoft. ".NET Documentation: File Upload Best Practices." https://docs.microsoft.com/dotnet/

[15] Various community benchmarks and discussions on Stack Overflow, GitHub, and technical blogs.

