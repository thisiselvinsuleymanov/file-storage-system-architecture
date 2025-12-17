# 1. Introduction

## 1.1 Background

The proliferation of user-generated content, document management systems, and media-rich applications has made efficient file storage a critical concern for modern web applications. As applications scale, the decision of where and how to store binary data becomes increasingly important, affecting performance, cost, maintainability, and user experience.

Traditional approaches have favored storing files directly in relational databases, leveraging transactional guarantees and simplified data models. However, as file sizes grow and storage requirements scale, alternative architectures using specialized object storage systems have gained prominence. These systems, exemplified by Amazon S3, Azure Blob Storage, and Google Cloud Storage, are designed specifically for large-scale binary data storage.

## 1.2 Problem Domain

ASP.NET applications, particularly those deployed in enterprise and government contexts, must balance multiple competing requirements:

- **Performance**: Fast upload and retrieval of files
- **Reliability**: Data durability and availability
- **Scalability**: Handling growing storage and traffic demands
- **Cost**: Efficient resource utilization
- **Operational Simplicity**: Manageable deployment and maintenance
- **Compliance**: Meeting regulatory and security requirements

The choice between database storage and object storage impacts all of these dimensions, yet clear guidance based on empirical evidence is often lacking.

## 1.3 Research Objectives

This research aims to:

1. **Quantify Performance Differences**: Measure upload throughput, read latency, and resource utilization for both storage strategies
2. **Analyze Hardware Implications**: Understand how storage decisions affect IO operations, write amplification, and cache behavior
3. **Provide Reproducible Framework**: Create a complete experimental setup that others can use to validate and extend findings
4. **Offer Practical Guidance**: Translate research findings into actionable recommendations for system architects

## 1.4 Scope and Limitations

This study focuses on:

- **Technology Stack**: ASP.NET Core, PostgreSQL, and Azure Blob Storage (via Azurite)
- **File Sizes**: Small (<100KB) to large (>10MB) binary files
- **Access Patterns**: Upload, download, and backup operations
- **Deployment Context**: Docker-based containerized environments

Limitations include:

- Single geographic region (Azerbaijan context)
- Emulated object storage (Azurite rather than production Azure)
- Limited to specific hardware configurations in test environment
- Focus on technical performance rather than cost analysis

## 1.5 Article Structure

The remainder of this article is organized as follows:

- **Section 2**: Related work and literature review
- **Section 3**: Problem statement and research questions
- **Section 4**: Hardware storage internals (PostgreSQL pages, TOAST, WAL)
- **Section 5**: System architecture and design
- **Section 6**: Experimental setup and methodology
- **Section 7**: Implementation details
- **Section 8**: Results and analysis
- **Section 9**: Discussion and interpretation
- **Section 10**: Real-world context (Azerbaijan deployment)
- **Section 11**: Ownership models and lessons learned
- **Section 12**: Conclusion
- **Section 13**: References

