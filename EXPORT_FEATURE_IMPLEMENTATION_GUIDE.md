# Export Feature - Implementation Guide

## Architecture Overview

This implementation follows Clean Architecture principles with clear separation of concerns:

### **Domain Layer** (`CleanArchitecture.Domain`)
- **Entity**: `ExportedFile` - Represents exported file metadata
- **Interfaces**:
  - `IFileStorageService` - Contract for file storage operations
  - `IExcelFileGenerator` - Contract for Excel generation
  - `IWordFileGenerator` - Contract for Word generation
  - `IPdfFileGenerator` - Contract for PDF generation

### **Application Layer** (`CleanArchitecture.Application`)
- **DTOs**:
  - `ExportDataRequest` - Request model for export operations
  - `ExportFileResponse` - Response model with file metadata
- **Interfaces**:
  - `IExportService` - Orchestration service
  - `IExportedFileRepository` - Data access for exports
- **Services**:
  - `ExportService` - Main orchestration logic

### **Infrastructure Layer** (`CleanArchitecture.Infrastructure`)
- **File Storage**: `MinIOFileStorageService` - MinIO implementation
- **File Generators**:
  - `ExcelFileGenerator` - Uses ClosedXML
  - `WordFileGenerator` - Uses OpenXML
  - `PdfFileGenerator` - Uses SelectPdf
- **Repository**: `ExportedFileRepository` - EF Core implementation
- **Configuration**: `ExportedFileConfiguration` - Entity mapping

### **API Layer** (`CleanArchitecture.Api`)
- **Controller**: `ExportController` - HTTP endpoints

## Component Responsibilities

### ExportController
- Validates incoming requests
- Extracts current user information
- Calls ExportService
- Returns appropriate HTTP responses

### ExportService
1. Validates bucket exists (creates if needed)
2. Generates file in requested format
3. Uploads to MinIO
4. Retrieves file size
5. Creates database record
6. Returns response with URL

### File Generators
- **ExcelFileGenerator**: Converts generic data to .xlsx using ClosedXML
- **WordFileGenerator**: Creates .docx with OpenXML SDK
- **PdfFileGenerator**: Converts HTML or Word to .pdf

### MinIOFileStorageService
- Manages MinIO client operations
- Generates presigned URLs (7-day expiry)
- Handles bucket creation
- Provides file operations (upload, delete, size)

### ExportedFileRepository
- Implements data access patterns
- Manages database persistence
- Provides queries by user/id/date

## Data Flow

```
1. Client sends ExportDataRequest to /api/export/data
   ├── Contains: FileName, Format, Data, Note, ExpiresAt
   └── Requires: JWT Token

2. ExportController validates and calls ExportService

3. ExportService orchestrates the export:
   ├── Ensure bucket exists
   ├── Generate file based on format
   │  ├── Excel: IExcelFileGenerator
   │  ├── Word: IWordFileGenerator
   │  └── PDF: IPdfFileGenerator
   ├── Upload to MinIO
   ├── Get file size
   ├── Create database record
   └── Return ExportFileResponse

4. Database record includes:
   ├── FileName: String
   ├── Url: Presigned MinIO URL
   ├── Bucket: "exports"
   ├── Size: File size in bytes
   ├── FileType: Excel/Word/PDF
   ├── ObjectName: MinIO path
   ├── UserId: Current user
   ├── Note: Export reason
   └── ExpiresAt: Optional expiration

5. ExportFileResponse returned to client with download URL
```

## Dependency Registration

The infrastructure layer registers all dependencies in `DependencyInjection.cs`:

```csharp
// Repositories
services.AddScoped<IExportedFileRepository, ExportedFileRepository>();

// File generators
services.AddScoped<IExcelFileGenerator, ExcelFileGenerator>();
services.AddScoped<IWordFileGenerator, WordFileGenerator>();
services.AddScoped<IPdfFileGenerator>(sp => 
    new PdfFileGenerator(configuration["PdfGenerator:LicenseKey"]));

// MinIO client
services.AddScoped<IMinioClient>(sp =>
    new MinioClient()
        .WithEndpoint(minioEndpoint)
        .WithCredentials(accessKey, secretKey)
        .Build());

// File storage service
services.AddScoped<IFileStorageService>(sp =>
    new MinIOFileStorageService(
        sp.GetRequiredService<IMinioClient>(),
        baseUrl));

// Main export service
services.AddScoped<IExportService, ExportService>(sp =>
    new ExportService(
        sp.GetRequiredService<IExportedFileRepository>(),
        sp.GetRequiredService<IFileStorageService>(),
        sp.GetRequiredService<IExcelFileGenerator>(),
        sp.GetRequiredService<IWordFileGenerator>(),
        sp.GetRequiredService<IPdfFileGenerator>(),
        sp.GetRequiredService<ICurrentUserService>(),
        minioBucket));
```

## Database Schema

### ExportedFiles Table

```sql
CREATE TABLE ExportedFiles (
    Id UUID PRIMARY KEY,
    FileName VARCHAR(255) NOT NULL,
    Url VARCHAR(2048) NOT NULL,
    Bucket VARCHAR(100) NOT NULL,
    Size BIGINT NOT NULL,
    FileType VARCHAR(50) NOT NULL,
    ObjectName VARCHAR(1024) NOT NULL,
    UserId UUID NOT NULL,
    Note VARCHAR(1000),
    ExpiresAt TIMESTAMP NULL,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NULL
);

-- Indexes for performance
CREATE INDEX IX_ExportedFiles_UserId 
    ON ExportedFiles(UserId);

CREATE INDEX IX_ExportedFiles_CreatedAt 
    ON ExportedFiles(CreatedAt);

CREATE INDEX IX_ExportedFiles_UserId_CreatedAt 
    ON ExportedFiles(UserId, CreatedAt);

CREATE INDEX IX_ExportedFiles_ExpiresAt 
    ON ExportedFiles(ExpiresAt);
```

## API Endpoints

### POST /api/export/data
Export data to specified format

**Request:**
```json
{
  "fileName": "Users Export",
  "format": 0,
  "data": [
    {"Id": "1", "Name": "John", "Email": "john@example.com"},
    {"Id": "2", "Name": "Jane", "Email": "jane@example.com"}
  ],
  "note": "User data export",
  "expiresAt": "2025-12-31T23:59:59Z"
}
```

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "fileName": "Users Export.xlsx",
  "url": "http://localhost:9000/exports/Users%20Export_20250101_120000.xlsx?X-Amz-Algorithm=...",
  "size": 102400,
  "fileType": "Excel",
  "createdAt": "2025-01-01T12:00:00Z",
  "expiresAt": "2025-12-31T23:59:59Z",
  "bucket": "exports"
}
```

### GET /api/export/{id}
Get export file details

**Response (200 OK):** Same as POST response above

### GET /api/export/my-exports
Get all exports for current user

**Response (200 OK):**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "fileName": "Users Export.xlsx",
    "url": "...",
    "size": 102400,
    "fileType": "Excel",
    "createdAt": "2025-01-01T12:00:00Z",
    "bucket": "exports"
  }
]
```

### DELETE /api/export/{id}
Delete export file

**Response (204 No Content)**

## Error Handling

The implementation includes comprehensive error handling:

- **400 Bad Request**: Invalid input, missing required fields
- **401 Unauthorized**: Missing or invalid JWT token
- **404 Not Found**: Export file not found
- **500 Internal Server Error**: MinIO connection, file generation, database errors

Example error response:
```json
{
  "message": "An error occurred while exporting data",
  "error": "Failed to upload file to MinIO: Connection refused"
}
```

## Extending the Feature

### Add New Export Format

1. Create interface in `Domain/Interfaces/Export/`:
```csharp
public interface INewFormatGenerator
{
    Task<Stream> GenerateAsync(...);
}
```

2. Implement in `Infrastructure/FileGeneration/`:
```csharp
public class NewFormatGenerator : INewFormatGenerator { ... }
```

3. Register in `DependencyInjection.cs`:
```csharp
services.AddScoped<INewFormatGenerator, NewFormatGenerator>();
```

4. Update `ExportService.GenerateFileAsync()` to handle new format

### Add Export Scheduling

Create background job using Hangfire:
```csharp
public class ExportCleanupJob
{
    public async Task CleanupExpiredExports()
    {
        var expiredExports = await _repository.GetExpiredAsync();
        foreach (var export in expiredExports)
        {
            await _fileStorageService.DeleteFileAsync(
                export.Bucket, export.ObjectName);
            await _repository.DeleteAsync(export.Id);
        }
    }
}
```

### Add Encryption

Implement encrypted storage:
```csharp
services.AddScoped<IEncryptionService, EncryptionService>();
// Use in ExportService to encrypt sensitive data
```

## Testing

### Unit Tests Example

```csharp
[TestFixture]
public class ExportServiceTests
{
    private Mock<IExportedFileRepository> _mockRepository;
    private Mock<IFileStorageService> _mockStorage;
    private Mock<IExcelFileGenerator> _mockExcel;
    private ExportService _service;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IExportedFileRepository>();
        _mockStorage = new Mock<IFileStorageService>();
        _mockExcel = new Mock<IExcelFileGenerator>();

        _service = new ExportService(
            _mockRepository.Object,
            _mockStorage.Object,
            _mockExcel.Object,
            // ... other mocks
        );
    }

    [Test]
    public async Task ExportDataAsync_ShouldCallGenerateFile()
    {
        // Arrange
        var request = new ExportDataRequest 
        { 
            Format = ExportFormat.Excel,
            FileName = "Test",
            Data = new[] { new Dictionary<string, object?> { { "Col", "Value" } } }
        };

        // Act
        await _service.ExportDataAsync(request, Guid.NewGuid());

        // Assert
        _mockExcel.Verify(x => x.GenerateAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<dynamic>>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

## Production Checklist

- [ ] Configure MinIO with SSL/TLS
- [ ] Set secure credentials in environment variables
- [ ] Configure database backups
- [ ] Implement export cleanup job for expired files
- [ ] Set up logging for all export operations
- [ ] Add rate limiting to export endpoints
- [ ] Implement audit logging for compliance
- [ ] Test with large datasets
- [ ] Load test MinIO and database
- [ ] Configure CloudFlare/CDN for file delivery
- [ ] Implement virus scanning for uploaded files
- [ ] Set up monitoring/alerting for export service
