# MBSD.CyberArk.CCPClient

A clean, simple, and robust .NET Standard library for integrating with CyberArk Central Credential Provider (CCP) with **maximum flexibility** for enterprise scenarios.

## ⚠️ Important Disclaimers

**This library is developed independently by Matthew Bohan and is not affiliated with, endorsed by, or sponsored by CyberArk / CyberArk Software Ltd.**

**CyberArk®, Central Credential Provider™, and related names are the property of CyberArk Software Ltd. or its affiliates.**

**This software uses only publicly available documentation and does not contain proprietary CyberArk code.**

---

## Features

- ✅ **Per-Request Flexibility** - Different Application IDs and certificates per request
- ✅ **Pre-Configured Mappings** - Map certificates to Application IDs for automatic selection
- ✅ **Simple API** - Clean and intuitive interface with fluent configuration
- ✅ **Multiple Authentication Methods** - Application ID only, client certificates (file or certificate store)
- ✅ **Async & Sync Support** - Both asynchronous and synchronous method variants
- ✅ **Comprehensive Logging** - Structured logging with Microsoft.Extensions.Logging
- ✅ **Flexible Configuration** - Support for configuration files, environment variables, etc.
- ✅ **SSL Configuration** - Control SSL verification and client certificates
- ✅ **Error Handling** - Comprehensive exception handling with detailed error information
- ✅ **.NET Standard 2.0** - Compatible with .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 5+

## Installation

```bash
dotnet add package MBSD.CyberArk.CCPClient
```

## Quick Start

### Per-Request Application IDs (Maximum Flexibility)

```csharp
using CyberArk.SecretsManager;
using CyberArk.SecretsManager.Configuration;
using CyberArk.SecretsManager.Models;

var options = new CCPOptions
{
    BaseUrl = "https://CCP.company.com"
    // No default Application ID needed!
};

using var httpClient = new HttpClient();
using var CCPClient = new CCPClient(httpClient, Options.Create(options));

// Async methods
var dbSecret = await CCPClient.GetSecretAsync(
    SecretRequest.ForObjectWithAppId("DatabaseAccount", "DatabaseApp"));

var adminSecret = await CCPClient.GetSecretAsync(
    SecretRequest.ForObjectWithAppId("AdminAccount", "AdminApp")
        .UsingCertificateFile(@"C:\certs\admin.p12", "password"));

// Synchronous methods (for legacy code compatibility)
var dbSecretSync = CCPClient.GetSecret(
    SecretRequest.ForObjectWithAppId("DatabaseAccount", "DatabaseApp"));

var passwordSync = CCPClient.GetPassword("SimpleAccount");

// Fluent API works with both async and sync
var password = await CCPClient.GetPasswordAsync(
    SecretRequest.ForObject("ServiceAccount")
        .UsingApplicationId("ServiceApp")
        .UsingCertificateStore("THUMBPRINT123")
        .InSafe("Production-Safe")
        .InFolder("Services"));
```

### Pre-Configured Application ID Mappings

```csharp
var options = new CCPOptions
{
    BaseUrl = "https://CCP.company.com",
    DefaultApplicationId = "WebApp",
    
    // Map certificates to Application IDs
    CertificatesByApplicationId = new Dictionary<string, CertificateConfig>
    {
        ["DatabaseApp"] = CertificateConfig.FromFile(@"C:\certs\database.p12", "dbpass"),
        ["AdminApp"] = CertificateConfig.FromStore("THUMBPRINT123"),
        ["ServiceApp"] = CertificateConfig.FromStore("THUMBPRINT456", StoreLocation.LocalMachine)
    }
};

// Now these automatically use the right certificates
var dbSecret = await CCPClient.GetSecretAsync(
    SecretRequest.ForObjectWithAppId("DatabaseAccount", "DatabaseApp")); // Uses database cert

var adminSecret = await CCPClient.GetSecretAsync(
    SecretRequest.ForObjectWithAppId("AdminAccount", "AdminApp")); // Uses admin cert from store
```

### Dependency Injection (Enterprise Recommended)

```csharp
using CyberArk.SecretsManager.Extensions;

// In Startup.cs or Program.cs
services.AddCyberArkCCP(options =>
{
    options.BaseUrl = "https://CCP.company.com";
    options.DefaultApplicationId = "WebApp";
    
    // Pre-configure certificates for different Application IDs
    options.CertificatesByApplicationId = new Dictionary<string, CertificateConfig>
    {
        ["TenantA"] = CertificateConfig.FromFile(@"C:\certs\tenantA.p12", "pass"),
        ["TenantB"] = CertificateConfig.FromFile(@"C:\certs\tenantB.p12", "pass"),
        ["AdminApp"] = CertificateConfig.FromStore("ADMIN_THUMBPRINT")
    };
});

// In your multi-tenant service
public class TenantService
{
    private readonly ICCPClient _CCPClient;

    public TenantService(ICCPClient CCPClient) => _CCPClient = CCPClient;

    public async Task<string> GetTenantDbPasswordAsync(string tenantId)
    {
        // Each tenant uses its own Application ID automatically
        return await _CCPClient.GetPasswordAsync(
            SecretRequest.ForObjectWithAppId($"{tenantId}_Database", tenantId));
    }
}
```

## Enterprise Scenarios

### Multi-Tenant Applications

```csharp
// Each tenant gets its own Application ID and certificate
var tenants = new[] { "TenantA", "TenantB", "TenantC" };

foreach (var tenant in tenants)
{
    var dbPassword = await CCPClient.GetPasswordAsync(
        SecretRequest.ForObjectWithAppId($"{tenant}_DB", $"{tenant}_App"));
    
    // Connect to tenant-specific database
    await ConnectToDatabase(tenant, dbPassword);
}
```

### Different Security Levels

```csharp
// Public data - no certificate required
var publicData = await CCPClient.GetSecretAsync(
    SecretRequest.ForObjectWithAppId("PublicConfig", "PublicApp"));

// Sensitive data - requires certificate
var sensitiveData = await CCPClient.GetSecretAsync(
    SecretRequest.ForObjectWithAppId("SensitiveAccount", "SensitiveApp"));
    // Certificate automatically selected from CertificatesByApplicationId

// Override certificate for specific request
var auditData = await CCPClient.GetSecretAsync(
    SecretRequest.ForObjectWithAppId("AuditAccount", "SensitiveApp")
        .UsingCertificateFile(@"C:\certs\audit-specific.p12", "auditpass"));
```

### Configuration from appsettings.json

```json
{
  "CyberArk": {
    "BaseUrl": "https://CCP.company.com",
    "DefaultApplicationId": "WebApp",
    "DefaultCertificate": {
      "FilePath": "C:\\certs\\default.p12",
      "Password": "defaultpass"
    },
    "CertificatesByApplicationId": {
      "DatabaseApp": {
        "FilePath": "C:\\certs\\database.p12",
        "Password": "dbpass"
      },
      "AdminApp": {
        "Thumbprint": "1234567890ABCDEF1234567890ABCDEF12345678",
        "StoreLocation": "LocalMachine",
        "StoreName": "My"
      },
      "TenantA": {
        "Thumbprint": "TENANT_A_THUMBPRINT",
        "StoreLocation": "CurrentUser"
      }
    }
  }
}
```

**Note**: In JSON configuration, use the enum names as strings:
- `StoreLocation`: `"CurrentUser"` or `"LocalMachine"`
- `StoreName`: `"My"`, `"Root"`, `"CA"`, `"Trust"`, `"Disallowed"`, `"TrustedPeople"`, `"TrustedPublisher"`, `"AuthRoot"`, `"AddressBook"`

```csharp
services.AddCyberArkCCP(configuration.GetSection("CyberArk"));
```

## Async vs Synchronous Usage

The library supports both asynchronous and synchronous calling patterns:

### Asynchronous Methods (Recommended)
```csharp
// Async methods - recommended for modern applications
var secret = await CCPClient.GetSecretAsync("DatabaseAccount");
var password = await CCPClient.GetPasswordAsync("APIKey");
bool isConnected = await CCPClient.TestConnectionAsync();

// Works great with ConfigureAwait for library code
var secret = await CCPClient.GetSecretAsync(request).ConfigureAwait(false);
```

### Synchronous Methods (Legacy Support)
```csharp
// Sync methods - useful for legacy code or console applications
var secret = CCPClient.GetSecret("DatabaseAccount");
var password = CCPClient.GetPassword("APIKey");
bool isConnected = CCPClient.TestConnection();

// Perfect for .NET Framework applications or where async isn't suitable
try
{
    var dbPassword = CCPClient.GetPassword("DatabaseAccount");
    ConnectToDatabase(dbPassword); // Legacy sync method
}
catch (CCPException ex)
{
    // Handle errors
}
```

### When to Use Each

**Use Async Methods When:**
- Building modern .NET Core/.NET 5+ applications
- In ASP.NET Core controllers or services
- When scalability and responsiveness are important
- In applications that already use async/await patterns

**Use Sync Methods When:**
- Working with legacy .NET Framework code
- In console applications where simplicity is preferred
- Integrating with existing synchronous APIs
- When the calling code doesn't support async patterns

**Note**: Synchronous methods use `.ConfigureAwait(false)` internally to avoid deadlocks in most scenarios.

## Authentication Priority

The library uses this priority order for Application ID and certificates:

### Application ID Priority:
1. **Request-specific** - `SecretRequest.ApplicationId`
2. **Default** - `CCPOptions.DefaultApplicationId`

### Certificate Priority:
1. **Request-specific** - `SecretRequest.Certificate`
2. **Application ID mapped** - `CCPOptions.CertificatesByApplicationId[applicationId]`
3. **Default** - `CCPOptions.DefaultCertificate`
4. **None** - No certificate (Application ID only authentication)

## Fluent API

The `SecretRequest` class provides a fluent API for building requests:

```csharp
var secret = await CCPClient.GetSecretAsync(
    SecretRequest.ForObject("MyAccount")
        .UsingApplicationId("MyApp")
        .UsingCertificateFile(@"C:\certs\cert.p12", "password")
        .InSafe("Production-Safe")
        .InFolder("Databases"));

// Or use certificate from store
var secret2 = await CCPClient.GetSecretAsync(
    SecretRequest.ForObject("MyAccount")
        .UsingApplicationId("MyApp")
        .UsingCertificateStore("THUMBPRINT123", StoreLocation.LocalMachine, StoreName.My)
        .InSafe("Production-Safe"));

// Chain multiple properties
var request = SecretRequest.ForObject("ServiceAccount")
    .UsingApplicationId("ServiceApp")
    .InSafe("Service-Safe")
    .InFolder("Production");
    
request.UserName = "serviceuser";
request.Address = "service.company.com";

var secret = await CCPClient.GetSecretAsync(request);
```

## Certificate Configuration

### From File
```csharp
var certConfig = CertificateConfig.FromFile(@"C:\certs\client.p12", "password");

// Or in options
options.DefaultCertificate = CertificateConfig.FromFile(@"C:\certs\default.p12", "pass");
```

### From Certificate Store
```csharp
using System.Security.Cryptography.X509Certificates;

var certConfig = CertificateConfig.FromStore(
    "1234567890ABCDEF1234567890ABCDEF12345678",
    StoreLocation.CurrentUser,  // or StoreLocation.LocalMachine
    StoreName.My);              // or StoreName.Root, StoreName.CA, etc.

// Or use defaults
var certConfig = CertificateConfig.FromStore("THUMBPRINT"); // CurrentUser\My
```

### Available Certificate Store Options

**StoreLocation enum values:**
- `StoreLocation.CurrentUser` - Current user's certificate store
- `StoreLocation.LocalMachine` - Local machine certificate store

**StoreName enum values:**
- `StoreName.My` - Personal certificates
- `StoreName.Root` - Trusted root certification authorities
- `StoreName.CA` - Intermediate certification authorities  
- `StoreName.Trust` - Trusted publishers
- `StoreName.Disallowed` - Revoked certificates
- `StoreName.TrustedPeople` - Trusted people
- `StoreName.TrustedPublisher` - Trusted publishers
- `StoreName.AuthRoot` - Third-party root certification authorities
- `StoreName.AddressBook` - Other people

## Error Handling

```csharp
try
{
    var password = await CCPClient.GetPasswordAsync(
        SecretRequest.ForObjectWithAppId("DatabaseAccount", "DatabaseApp"));
}
catch (CCPException ex)
{
    Console.WriteLine($"CCP Error: {ex.Message}");
    Console.WriteLine($"Application ID: {ex.ApplicationId}");
    Console.WriteLine($"HTTP Status: {ex.HttpStatusCode}");
    Console.WriteLine($"Error Code: {ex.ErrorCode}");
    Console.WriteLine($"Response: {ex.ResponseContent}");
}
```

## Configuration Options

| Property | Description | Default |
|----------|-------------|---------|
| `BaseUrl` | CCP server URL | Required |
| `DefaultApplicationId` | Default Application ID | Optional |
| `Endpoint` | CCP API endpoint path | `/AIMWebService/api/Accounts` |
| `TimeoutSeconds` | HTTP request timeout | `30` |
| `VerifySsl` | Verify SSL certificates | `true` |
| `DefaultCertificate` | Default certificate configuration | `null` |
| `CertificatesByApplicationId` | Application ID to certificate mappings | `{}` |

## API Methods

The library provides both asynchronous and synchronous versions of all methods:

### Asynchronous Methods
- `GetSecretAsync(SecretRequest request)` - Get full secret details
- `GetSecretAsync(string objectName)` - Get secret by name  
- `GetPasswordAsync(SecretRequest request)` - Get password only
- `GetPasswordAsync(string objectName)` - Get password by name
- `TestConnectionAsync()` - Test CCP connectivity

### Synchronous Methods
- `GetSecret(SecretRequest request)` - Get full secret details
- `GetSecret(string objectName)` - Get secret by name
- `GetPassword(SecretRequest request)` - Get password only  
- `GetPassword(string objectName)` - Get password by name
- `TestConnection()` - Test CCP connectivity

## Performance Considerations

- **Certificate Caching**: The client automatically caches HTTP clients by certificate configuration
- **Thread Safety**: The client is fully thread-safe
- **Connection Reuse**: Uses HttpClient connection pooling
- **Memory Management**: Properly disposes cached HTTP clients

## Migration from Fixed Application ID

If you have existing code with a fixed Application ID, migration is easy:

```csharp
// Old way (still works)
var options = new CCPOptions 
{ 
    BaseUrl = "https://CCP.company.com",
    DefaultApplicationId = "MyApp" 
};

var secret = await CCPClient.GetSecretAsync("DatabaseAccount");

// New flexible way
var secret = await CCPClient.GetSecretAsync(
    SecretRequest.ForObjectWithAppId("DatabaseAccount", "DatabaseApp"));
```

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## Legal Information

This library is developed independently by Matthew Bohan and is not affiliated with, endorsed by, or sponsored by CyberArk / CyberArk Software Ltd.

CyberArk®, Central Credential Provider™, and related names are the property of CyberArk Software Ltd. or its affiliates.

This software uses only publicly available documentation and does not contain proprietary CyberArk code.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes with tests
4. Submit a pull request

Please ensure all contributions comply with the Apache License 2.0 and do not include any proprietary CyberArk code or documentation.

## Support

For issues and questions:
- GitHub Issues: [Report bugs and request features](https://github.com/matthewbohan/mbsd-cyberark-CCPclient/issues)
- CyberArk Documentation: [Official CCP documentation](https://docs.cyberark.com/)

**Note**: This is an independent library. For official CyberArk support, please contact CyberArk directly.
