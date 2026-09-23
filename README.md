## Distributed Order Processing Engine

An enterprise-grade, distributed microservices architecture built with .NET 9, designed for high-throughput order processing, strict transactional integrity, and reliable messaging.

This project solves the classic dual-write problem (coordinating database writes and message broker publishing) by implementing the Transactional Outbox Pattern using MassTransit and Entity Framework Core.

### System Architecture

The solution is decoupled into independent microservices communicating asynchronously via RabbitMQ:

[ Client / API Request ]
│
▼
┌───────────────────────────────────────────────┐
│ OrderService (Producer) │
│ ├─► PostgreSQL (Orders Table) │
│ └─► PostgreSQL (MassTransit Outbox Tables) │
└───────────────────────┬───────────────────────┘
│ (Transactional Dispatch)
▼
[ RabbitMQ ]
│
▼
┌───────────────────────────────────────────────┐
│ NotificationService (Consumer) │
│ └─► Processes OrderSubmitted Events │
└───────────────────────────────────────────────┘

- OrderService (Producer): Exposes REST APIs via native .NET 9 OpenAPI. Accepts order submissions, atomically persists records to PostgreSQL, and queues corresponding events into the outbox table within the exact same database transaction.

- NotificationService (Consumer): Listens asynchronously to event streams via RabbitMQ, guaranteeing at-least-once delivery semantics and decoupling downstream notifications from core order placement.

- Resilience: Background outbox dispatchers safely poll the database table to push messages to RabbitMQ, ensuring zero message loss even during temporary network interruptions or broker restarts.

### Technology Stack

- Core Framework: .NET 9 (C#)
- Messaging: RabbitMQ, MassTransit (v8.3.6)
- Persistence: PostgreSQL, Entity Framework Core 9 (with EF Outbox configuration)
- API & Tooling: Native .NET 9 OpenAPI (Microsoft.AspNetCore.OpenApi)
- Infrastructure: Docker & Docker Compose

### Repository Structure

OrderEngine/
├── OrderService/ # Producer API & Database context (PostgreSQL + Outbox)
├── NotificationService/ # Consumer worker service handling event streams
├── SharedContracts/ # Shared message contracts & DTOs
├── docker-compose.yml # Local infrastructure (PostgreSQL & RabbitMQ)
└── README.md

### Getting Started Guide

Prerequisites:

- .NET 9 SDK installed locally
- Docker Desktop running

1. Launch Infrastructure
   Spin up PostgreSQL and RabbitMQ via Docker Compose from the root directory:
   docker compose up -d

2. Run the Microservices
   Open separate terminal windows for each service to run them concurrently:

Start the Order Service (Producer):
dotnet run --project OrderService/OrderService.csproj

(Note: On initial startup, the service automatically provisions the PostgreSQL database schema, relational tables, and outbox indices).

Start the Notification Service (Consumer):
dotnet run --project NotificationService/NotificationService.csproj

### API Reference

Submit an Order

- Endpoint: POST /orders
- Content-Type: application/json

Request Payload:
{
"customerId": "CUST-ENG-001",
"totalAmount": 1500.00
}

Response (202 Accepted):

{
"orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
"status": "Submitted"
}

### Engineering Highlights

- Zero Dual-Write Failures: Utilizing the EF Core Outbox pattern ensures that database commits and event dispatches are entirely atomic.

- Modern .NET 9 Tooling: Fully integrated with native OpenAPI endpoints (/openapi/v1.json) for lightweight, high-performance API documentation.

- Secure Configuration: Environment-variable-driven infrastructure configurations with interactive terminal password prompts for database authentication fallback.
