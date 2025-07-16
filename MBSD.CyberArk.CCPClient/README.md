# MBSD.CyberArk.CCPClient

A clean, simple, and robust .NET Standard library for integrating with CyberArk Central Credential Provider (CCP) with **maximum flexibility** for enterprise scenarios.

## ⚠️ Important Disclaimers

**This library is developed independently by Matthew Bohan and is not affiliated with, endorsed by, or sponsored by CyberArk / CyberArk Software Ltd.**

**CyberArk®, Central Credential Provider™, and related names are the property of CyberArk Software Ltd. or its affiliates.**

**This software uses only publicly available documentation and does not contain proprietary CyberArk code.**

---


## This project is a work in progress  
This is an initial version of the library, and it may not be fully functional or tested. I don't have access to a CyberArk instance from my personal computer, so contributions, feedback, and testing are welcome to help improve it.



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
