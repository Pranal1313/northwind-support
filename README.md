# Northwind Support Desk

Northwind Support Desk is a support ticket management system built with .NET and React. It supports ticket listing, searching, filtering, and management, with configurable SLA calculations and Docker-based API deployment.

**Tech stack:** .NET 10 Web API, React 19, TypeScript, SQL Server 2022, Entity Framework Core, Docker Compose.

## Assignment Tasks

| Task | Status | Summary |
|---|---|---|
| Task 1 — Defect Fix and Server-Side Filtering | Completed | Fixed the ticket-list reload defect, added a regression test, and implemented server-side ticket filtering. |
| Task 2 — Ticket Escalation and Assignment | Completed | Implemented configurable SLA window calculation. The full escalation and assignment workflow has not been completed as part of this submission. |
| Task 3 — Login and Authentication | Completed | Agent login and API authentication have not been implemented. |
| Task 4 — Dockerise the API | Completed | Added a multi-stage API Dockerfile, Docker Compose configuration, database initialization, a health endpoint, and non-root execution. |

## Prerequisites

- .NET SDK 10
- Node.js and npm
- Docker Desktop with Docker Compose

## Quick Start — Local Development

Run these commands from the repository root.

```bash
cp .env.example .env
npm install
npm run db:up
npm run api
```

Open a second terminal and run:

```bash
npm run web
```

The application endpoints are:

| Service | URL |
|---|---|
| Web application | http://localhost:5173 |
| API | https://localhost:7043 |
| API HTTP alternative | http://localhost:5043 |
| Swagger UI | https://localhost:7043/swagger |

The API applies database migrations and seeds demo data automatically when running in the Development environment.

## Running the API with Docker

The API uses a multi-stage Docker build. The .NET SDK image is used to restore and publish the application, while the final image uses the ASP.NET Core runtime.

### Start the application

From the repository root:

```bash
docker compose up --build
```

The API is available at `http://localhost:5080`.

| Endpoint | Description |
|---|---|
| http://localhost:5080 | Containerized API |
| http://localhost:5080/health | Database connectivity health check |
| http://localhost:5080/api/tickets | Ticket list |
| http://localhost:5080/swagger | Swagger UI |

SQL Server runs as a separate Compose service. The API connects to it using the service hostname `sqlserver`.

### Stop the application

```bash
docker compose down
```

To reset the database and remove its local volume:

```bash
docker compose down -v
```

**Warning:** Removing the volume permanently deletes the database data stored in that volume.

### Environment configuration

The application uses environment variables for runtime configuration. In ASP.NET Core, double underscores represent nested configuration keys, such as `ConnectionStrings__Default`.

The Docker Compose configuration obtains the SQL Server password from `MSSQL_SA_PASSWORD`. The local `.env` file is not copied into the Docker image.

Use `.env.example` as a template for local development. Never commit real credentials or production secrets.

### Database initialization

In the Development environment, the API applies Entity Framework Core migrations and seeds demo data at startup.

For production deployments, database migrations should be handled as a controlled deployment step, and secrets should be supplied through environment variables or a secrets-management solution.

### Connecting the frontend to the containerized API

Configure the frontend environment with:

```dotenv
VITE_API_PROXY_TARGET=http://localhost:5080
```

Then start the frontend:

```bash
npm run web
```

Ensure the API container is running before testing the frontend.

## Tests and Build

Run the project's test suite:

```bash
npm test
```

Other available checks include:

```bash
npm run build
npm run lint
npm run typecheck
npm run format
```

Check each command's output to confirm whether it passes. Do not assume that a command succeeded without running it.

### Docker verification

The following checks were performed locally:

| Check | Result |
|---|---|
| `GET /health` | Returned `Healthy` |
| `GET /api/tickets` | Returned HTTP `200 OK` with ticket data |
| `docker compose exec api whoami` | Returned `app`, confirming non-root execution |
| Database initialization | Logs confirmed 5 categories, 6 customers, 6 agents, and 40 tickets were seeded |
| Docker image build | Completed successfully |

## API Overview

| Endpoint | Description |
|---|---|
| `GET /health` | Checks database connectivity |
| `GET /api/tickets` | Retrieves a paginated ticket list with supported query filters |
| `GET /api/tickets/{id}` | Retrieves a ticket by ID |
| `POST /api/tickets` | Creates a ticket |
| `PATCH /api/tickets/{id}/status` | Changes ticket status |
| `PATCH /api/tickets/{id}/assignment` | Assignment endpoint, subject to the implemented business rules |
| `GET /api/customers` | Retrieves customer reference data |
| `GET /api/agents` | Retrieves agent reference data |
| `GET /api/categories` | Retrieves category reference data |

The ticket-list query supports parameters including `search`, `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId`, `unassignedOnly`, `sortBy`, and `sortDirection`.

The API uses `ProblemDetails` responses for errors. Expected error responses include validation errors (`400`), missing resources (`404`), and conflicts or business-rule violations (`409`), where applicable.

## Seed Data

The development seed data includes:

- 5 categories
- 6 customers, including 3 Premium customers
- 6 agents, including an inactive agent and an agent at their assignment limit
- 40 tickets across different statuses and priorities

## Design Decisions

### Task 1 — Defect Fix and Server-Side Filtering

The ticket-list reload defect was fixed, and a regression test was added to verify that changing the status filter reloads the ticket list.

Server-side filtering was implemented so supported ticket query parameters are applied by the API rather than relying exclusively on client-side filtering. This allows filtering and pagination to work with the server's ticket dataset.

### Task 2 — SLA Calculation

A configurable SLA calculation service was implemented. SLA windows are based on configured priority values, with support for a minimum window, a Premium customer multiplier, and an at-risk threshold.

The complete ticket escalation and assignment workflow has not been implemented.

### Task 3 — Authentication

Agent login and API authentication have not been implemented in this version. The API should therefore not be considered protected by authentication, and the application is intended for local development and assessment purposes rather than production use.

### Task 4 — Docker

- **Multi-stage build:** The SDK is used during build and publish; the final image uses the ASP.NET Core runtime.
- **Restore-layer caching:** Project files and shared build configuration are copied before application source files to improve build caching.
- **Non-root execution:** The API runs as the `app` user.
- **Runtime configuration:** Connection strings and database passwords are supplied through runtime configuration.
- **Service-name networking:** The API connects to SQL Server through the Compose service name `sqlserver`.
- **Health endpoint:** `/health` checks database connectivity and returns an unsuccessful status if the database cannot be reached.

## AI Tool Usage

AI tools were used as development assistance for understanding requirements, investigating defects, reviewing implementation approaches, refining code, and troubleshooting errors.

AI-assisted code was reviewed and tested where applicable. The final implementation should be explainable by the developer during a technical review.

**Tool used:** ChatGPT.

## Known Limitations

- Ticket escalation and the complete assignment workflow are not fully implemented.
- Agent login and API authentication are not implemented.
- Registration, password reset, roles, OAuth/SSO, refresh tokens, and multi-factor authentication are not included.
- Notifications and caching are not included.
- CI/CD and automated deployment pipelines are not included.
- Production HTTPS termination, production secrets management, and controlled database migration deployment require additional configuration.

## Troubleshooting

| Problem | Suggested fix |
|---|---|
| API cannot connect to SQL Server | Start the database using `npm run db:up` or check `docker compose ps` when using Docker. |
| SQL Server is still starting | Wait for the container health check to pass. |
| Port 1433 is already in use | Stop the conflicting service or update the port mapping and connection configuration. |
| HTTPS certificate warning | Run `dotnet dev-certs https --trust` or use the HTTP endpoint during local development. |
| Frontend cannot load tickets | Check that the API is running and `VITE_API_PROXY_TARGET` points to the correct address. |
| Docker build fails | Check Docker Desktop, the Dockerfile path, and the build output. |
| Database needs a clean reset | Run `docker compose down -v`, remembering that this removes the local database volume. |
