# Export Feature - MinIO Integration Configuration

## appsettings.json Configuration

Add the following configuration to your `appsettings.json` file:

```json
{
  "MinIO": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "Bucket": "exports",
    "BaseUrl": "http://localhost:9000"
  },
  "PdfGenerator": {
    "LicenseKey": null
  }
}
```

## appsettings.Production.json Configuration

For production environment:

```json
{
  "MinIO": {
    "Endpoint": "minio.yourdomain.com:9000",
    "AccessKey": "your-access-key",
    "SecretKey": "your-secret-key",
    "Bucket": "exports",
    "BaseUrl": "https://minio.yourdomain.com"
  },
  "PdfGenerator": {
    "LicenseKey": "your-license-key"
  }
}
```

## Environment Variables (Alternative)

Set these environment variables instead of using appsettings.json:

```bash
MINIO_ENDPOINT=localhost:9000
MINIO_ACCESSKEY=minioadmin
MINIO_SECRETKEY=minioadmin
MINIO_BUCKET=exports
MINIO_BASEURL=http://localhost:9000
PDF_GENERATOR_LICENSEKEY=your-license-key
```

## NuGet Packages Required

Add these packages to `CleanArchitecture.Infrastructure.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Minio" Version="6.1.0" />
  <PackageReference Include="ClosedXML" Version="0.104.1" />
  <PackageReference Include="DocumentFormat.OpenXml" Version="3.1.0" />
  <PackageReference Include="SelectPdf" Version="25.1.1" />
</ItemGroup>
```

## Database Migration

Create a new migration to add the ExportedFiles table:

```bash
# From Infrastructure project directory
dotnet ef migrations add AddExportedFiles
dotnet ef database update
```

Generated migration will include the ExportedFiles table with proper indexes.

## MinIO Setup (Local Development)

### Using Docker Compose:

```yaml
version: '3.8'
services:
  minio:
    image: minio/minio:latest
    ports:
      - "9000:9000"
      - "9001:9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
    volumes:
      - minio_storage:/minio_data
    command: server /minio_data --console-address ":9001"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]
      interval: 30s
      timeout: 20s
      retries: 3

volumes:
  minio_storage:
```

Then run:

```bash
docker-compose up -d
```

Access MinIO Console at `http://localhost:9001`
- Username: minioadmin
- Password: minioadmin

## Usage Example

### Export to Excel:

```csharp
var request = new ExportDataRequest
{
    FileName = "Users Export",
    Format = ExportFormat.Excel,
    Data = users.Select(u => new Dictionary<string, object?>
    {
        { "Id", u.Id },
        { "Name", u.Name },
        { "Email", u.Email },
        { "CreatedAt", u.CreatedAt }
    }),
    Note = "User export for analysis",
    ExpiresAt = DateTime.UtcNow.AddDays(7)
};

var response = await _exportService.ExportDataAsync(request, userId);
// Returns: ExportFileResponse with Url, FileName, Size, etc.
```

### API Endpoint Usage:

```bash
# Export data
curl -X POST http://localhost:5000/api/export/data \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fileName": "Users Export",
    "format": 0,
    "data": [
      {"Id": "1", "Name": "John", "Email": "john@example.com"}
    ],
    "note": "User export",
    "expiresAt": "2025-12-31T23:59:59Z"
  }'

# Get all my exports
curl -X GET http://localhost:5000/api/export/my-exports \
  -H "Authorization: Bearer YOUR_TOKEN"

# Get specific export
curl -X GET http://localhost:5000/api/export/{id} \
  -H "Authorization: Bearer YOUR_TOKEN"

# Delete export
curl -X DELETE http://localhost:5000/api/export/{id} \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Key Features

✅ **Clean Architecture**: Separation of concerns across Domain, Application, and Infrastructure layers
✅ **Dependency Injection**: All services properly injected via DI container
✅ **Async/Await**: Fully asynchronous operations
✅ **Multiple Formats**: Excel, Word, PDF generation
✅ **MinIO Integration**: Secure file storage with presigned URLs
✅ **Database Persistence**: File metadata tracked with proper indexes
✅ **Error Handling**: Comprehensive exception handling and validation
✅ **Entity Framework**: DbSet and configuration properly configured
✅ **Audit Trail**: CreatedAt/UpdatedAt tracking on all exports

## Architecture Flow

```
Client Request
    ↓
ExportController (API Layer)
    ↓
ExportService (Application Layer)
    ↓
FileGenerator (Excel/Word/PDF)
    ↓
FileStorageService (MinIO)
    ↓
ExportedFileRepository (Data Layer)
    ↓
ExportedFile Entity (Domain)
    ↓
Database (PostgreSQL)
```

## Security Considerations

1. **Authorization**: All endpoints require JWT token
2. **User Isolation**: Exports are associated with UserId
3. **Presigned URLs**: MinIO URLs are time-limited (7 days)
4. **File Validation**: Content type and size validation
5. **Sensitive Data**: Consider implementing file expiration cleanup job

## Performance Optimization

1. **Indexes**: Database queries optimized with indexes on UserId, CreatedAt, ExpiresAt
2. **Async Operations**: All I/O operations are asynchronous
3. **Stream Processing**: Files generated as streams to avoid memory issues
4. **Lazy Loading**: Only load required data from database

## Troubleshooting

### MinIO Connection Issues:
- Verify MinIO is running: `http://localhost:9000/minio/health/live`
- Check credentials in appsettings.json
- Ensure endpoint format is correct (with or without https)

### PDF Generation Issues:
- Install required dependencies for SelectPdf
- Verify license key if using commercial version
- Check temp directory write permissions

### File Upload Issues:
- Ensure bucket exists (auto-created on first use)
- Check MinIO storage quota
- Verify network connectivity to MinIO

### Database Issues:
- Run migrations: `dotnet ef database update`
- Check PostgreSQL connection string
- Verify DbContext is properly configured
