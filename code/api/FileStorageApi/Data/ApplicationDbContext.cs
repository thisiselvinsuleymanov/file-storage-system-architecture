using FileStorageApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStorageApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<FileEntity> Files { get; set; }
    public DbSet<FileBlobEntity> FilesBlob { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Database storage table
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.ToTable("files_db");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Filename)
                .HasColumnName("filename")
                .HasMaxLength(255).IsRequired();
            entity.Property(e => e.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(100).IsRequired();
            entity.Property(e => e.FileSize)
                .HasColumnName("file_size")
                .IsRequired();
            entity.Property(e => e.UploadedAt)
                .HasColumnName("uploaded_at")
                .IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.FileData)
                .HasColumnName("file_data")
                .HasColumnType("bytea").IsRequired();
            
            entity.HasIndex(e => e.UploadedAt);
            entity.HasIndex(e => e.Filename);
        });

        // Object storage metadata table
        modelBuilder.Entity<FileBlobEntity>(entity =>
        {
            entity.ToTable("files_blob");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Filename)
                .HasColumnName("filename")
                .HasMaxLength(255).IsRequired();
            entity.Property(e => e.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(100).IsRequired();
            entity.Property(e => e.FileSize)
                .HasColumnName("file_size")
                .IsRequired();
            entity.Property(e => e.UploadedAt)
                .HasColumnName("uploaded_at")
                .IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.BlobContainer)
                .HasColumnName("blob_container")
                .HasMaxLength(100).IsRequired();
            entity.Property(e => e.BlobName)
                .HasColumnName("blob_name")
                .HasMaxLength(500).IsRequired();
            
            entity.HasIndex(e => e.UploadedAt);
            entity.HasIndex(e => e.Filename);
            entity.HasIndex(e => new { e.BlobContainer, e.BlobName }).IsUnique();
        });
    }
}

