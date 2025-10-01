# AGTecFramework

A comprehensive collection of NuGet packages designed to help build .NET microservice-based applications following CQRS (Command Query Responsibility Segregation) principles.

## Overview

AGTecFramework provides a modular set of packages that streamline the development of modern, scalable microservices in .NET. The framework promotes clean architecture, separation of concerns, and event-driven design patterns.

## Package Categories

### Core CQRS Packages

#### AGTec.Common.CQRS
The foundation package providing core CQRS implementation with:
- **Commands**: Write operations that modify state
- **Queries**: Read operations that return data
- **Events**: Domain events for event-driven architecture
- **Dispatchers**: Command, Query, and Event dispatchers for routing requests to handlers
- **Handlers**: Interfaces for implementing command, query, and event handlers

#### AGTec.Common.CQRS.Messaging.AzureServiceBus
Azure Service Bus integration for CQRS messaging:
- Message publishing and handling
- Event dispatching over Azure Service Bus
- Message broker implementation for distributed systems

#### AGTec.Common.CQRS.Messaging.JsonSerializer
JSON serialization support for CQRS messages:
- JSON-based message and payload serialization
- Seamless integration with CQRS messaging infrastructure

#### AGTec.Common.CQRS.Messaging.ProtoBufSerializer
Protocol Buffers serialization for CQRS messages:
- High-performance binary serialization
- Efficient message payload handling

### Domain & Repository Packages

#### AGTec.Common.Base
Foundation package providing base types and utilities:
- Value objects
- Specifications pattern
- Extensions and utilities

#### AGTec.Common.Domain
Domain-driven design support:
- Domain entities and aggregates
- Rich domain models

#### AGTec.Common.Repository
Generic repository pattern implementation:
- Read-only and full repositories
- Change tracking support
- Entity Framework Core integration

#### AGTec.Common.Repository.Document
MongoDB document database repository:
- Document-based data access
- MongoDB collection management
- Document entity support

#### AGTec.Common.Repository.Search
Search functionality for repositories:
- Advanced search capabilities
- Query optimization

### Supporting Packages

#### AGTec.Common.Document
Document entity definitions and attributes for NoSQL databases.

#### AGTec.Common.BackgroundTaskQueue
Background task processing:
- Queued task execution
- Hosted service for background jobs

#### AGTec.Common.Randomizer
Data generation and randomization utilities:
- Test data generation
- Random value generators

#### AGTec.Common.Test
Testing utilities and base classes:
- Auto-mocking support
- Base test classes
- Testing specifications

### Service Infrastructure

#### AGTec.Services.ServiceDefaults
Service infrastructure and defaults:
- OpenTelemetry integration for observability
- Health checks
- Service discovery
- HTTP resilience patterns
- Azure Monitor integration
- Background task queue hosting

## Getting Started

### Installation

Install packages from NuGet based on your needs:

```bash
# Core CQRS
dotnet add package AGTec.Common.CQRS

# With Azure Service Bus messaging
dotnet add package AGTec.Common.CQRS.Messaging.AzureServiceBus

# Repository pattern
dotnet add package AGTec.Common.Repository

# Service defaults for microservices
dotnet add package AGTec.Services.ServiceDefaults
```

### Basic Usage

#### Setting up CQRS

```csharp
// In your Startup.cs or Program.cs
services.AddCQRS();
```

#### Setting up CQRS with Azure Service Bus

```csharp
services.AddCQRSWithMessaging("your-azure-servicebus-connection-string");
```

#### Using Service Defaults

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add service defaults with health checks, telemetry, and resilience
builder.AddServiceDefaults();
```

## Architecture

The framework follows these key principles:

1. **CQRS Pattern**: Separate command and query responsibilities
2. **Event-Driven**: Support for domain events and event sourcing
3. **Microservices-Ready**: Designed for distributed, scalable architectures
4. **Clean Architecture**: Clear separation of concerns
5. **Testability**: Built-in support for testing with mocking utilities

## Building from Source

```bash
# Build all projects
dotnet build AGTecFramework.sln

# Build and publish packages (requires NuGet repository configuration)
./build/BuildAndPublish.sh [Release|Debug] [NugetRepo] [NugetRepoApiKey]
```

## Target Framework

All packages target **.NET 9.0**.

## Repository

Source code: [https://github.com/victorgreggio/AGTecFramework](https://github.com/victorgreggio/AGTecFramework)

## Author

Victor Greggio

## License

Check the repository for license information.