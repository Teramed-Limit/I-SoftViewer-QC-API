# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ISoftViewerQCSystem is a .NET 6 Web API application for DICOM (Digital Imaging and Communications in Medicine) Quality Control operations. It provides RESTful APIs for managing medical imaging studies, quality control processes, and PACS (Picture Archiving and Communication System) integration.

## Development Commands

### Build and Run
```bash
# Build the solution
dotnet build ISoftViewerQCSystem.sln

# Run the application in development mode
dotnet run --project ISoftViewerQCSystem/ISoftViewerQCSystem.csproj

# Run with specific environment
dotnet run --project ISoftViewerQCSystem/ISoftViewerQCSystem.csproj --environment Development
```

### Docker Commands
```bash
# Build Docker image
docker build -t isoftviewer-qc-system .

# Run Docker container
docker run -p 5000:80 -p 5001:443 isoftviewer-qc-system
```

### Testing and Debugging
- No test projects are currently configured in this solution
- Application logs are written to `./Logs/` directory with daily rolling files
- Swagger UI is available at `/swagger` endpoint in development mode

## Architecture Overview

### Application Layer Pattern
The system follows a layered architecture with clear separation of concerns:

- **Controllers**: HTTP endpoints handling API requests (`/Controllers/`)
- **Applications**: Business logic and orchestration services (`/Applications/`)
- **Services**: Domain services and repository implementations (`/Services/`)
- **Models**: Data transfer objects and value objects (`/Models/`)
- **Hubs**: SignalR hubs for real-time communication (`/Hubs/`)

### Core Dependencies
- **ISoftViewerLibrary**: External library providing DICOM operations, database models, and domain services
- **AutoMapper**: Object-to-object mapping for DTOs
- **Serilog**: Structured logging with file and console outputs
- **JWT Authentication**: Token-based authentication (currently commented out)
- **SignalR**: Real-time communication framework (ChatHub implementation)

### Key Services and Controllers

#### Quality Control Operations
- `QualityControlController`: Primary QC operations for DICOM images
- `StudyQcApplicationService`: Application service for study-level QC operations
- `QCOperationContext`: Handles QC operation logging and context

#### DICOM Management
- `DicomTagController`: DICOM tag manipulation and queries
- `DcmDataQueryApplicationService`: DICOM data querying operations
- `DcmDataCmdApplicationService`: DICOM data command operations

#### PACS Integration
Controllers in `/Controllers/PacsServer/`:
- `PacsDestinationNodesController`: PACS destination node management
- `PacsServiceProviderController`: PACS service provider configuration
- `PacsStorageDeviceController`: Storage device management

### Database Configuration
- SQL Server database with configurable connection strings
- Custom repository pattern implementation through `ICommonRepositoryService<T>`
- Database configuration in `appsettings.json` under `Database` section
- Entity mappings configured in `ServiceMappings.cs`

### Authentication and Authorization
- JWT token configuration available (currently disabled in Startup.cs)
- `AuthService` provides authentication logic
- User account management through `UserAccountController`

### Real-time Communication
- SignalR implementation with `ChatHub`
- Connection mapping service for managing user connections
- Room-based communication system

## Configuration

### Key Configuration Sections
- `Database`: SQL Server connection settings
- `JWT`: Authentication token configuration
- `Serilog`: Logging configuration with file rotation
- `DcmTagMappingTable`: DICOM tag mapping configurations
- `VirtualFilePath`: File path for image serving

### Environment Settings
- Development: Swagger UI enabled, detailed logging
- Production: Static file serving from `ClientApp/build`
- Docker: Windows container support configured

### External Dependencies
- Custom DLLs in `/dll/`: `MQDLL.dll`, `amqmdnet.dll` (IBM MQ components)
- ISoftViewerLibrary project reference for core DICOM functionality

## Important Notes

### DICOM Operations
- All DICOM operations should go through the ISoftViewerLibrary
- Study/Series/SOPInstance UIDs are used as primary identifiers
- Quality control operations are logged through `QCOperationContext`

### Database Operations
- Use dependency-injected services rather than direct database access
- Repository pattern implemented through `ICommonRepositoryService<T>`
- Database tables follow specific naming conventions (Svr prefix for server tables)

### Error Handling
- Structured logging through Serilog
- BadRequestResult model for standardized error responses
- Exception logging configured in Program.cs

### Code Style
- XML documentation required for public APIs
- Editor config disables CS1591 (missing XML comments) and CA1416 (platform compatibility) warnings
- Camelcase parameter transformer applied to all controllers