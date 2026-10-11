# Northwind Support Desk

A support ticket management system for creating, searching, filtering, assigning, and managing support tickets through their lifecycle.

**Tech stack:** .NET 10 Web API, React 19, TypeScript, SQL Server 2022, Entity Framework Core, Docker Compose.

## Assignment Tasks

| Task | Status | Summary |
|---|---|---|
| Task 1 — Defect Fix and Server-Side Filtering | Completed / Verify | Fixed the ticket-list reload defect and added a regression test. Server-side filtering was implemented for the supported ticket query parameters. |
| Task 2 — Ticket Escalation and Assignment | Update before submission | Automatic triage, escalation history, SLA status, and related UI. Describe only the features actually implemented. |
| Task 3 — Login and Authentication | Update before submission | Agent login, API authentication, and login UI. Describe only the features actually implemented. |
| Task 4 — Dockerise the API | Completed | Multi-stage API image, SQL Server dependency, runtime database migration and seeding in Development, health endpoint, and non-root execution. |

## Prerequisites

- .NET SDK 10
- Node.js 22 (Node.js 20.19+ may also work)
- npm
- Docker Desktop with Docker Compose

## Quick Start — Local Development

From the repository root:

```bash
cp .env.example .env
npm install
npm run db:up
npm run api
```

In a second terminal, run:

```bash
npm run web
```

The application should be available at:

| Service | URL |
|---|---|
| Web application | http://localhost:5173 |
| API | https://localhost:7043 |
| API HTTP alternative | http://localhost:5043 |
| Swagger UI | https://localhost:7043/swagger |

The API applies database migrations and seeds demo data automatically when running in the Development environment.

Never commit real credentials or secret keys.

## Running the API with Docker

The API uses a multi-stage Docker build. The SDK image is used to restore, build, and publish the application. The final image uses the ASP.NET Core runtime image, keeping the SDK out of the runtime container.

### Start the application

From the repository root:

```bash
docker compose up --build
```

The API is exposed on `http://localhost:5080`. SQL Server is available to the API through the Docker Compose service name `sqlserver`.

| Service | URL |
|---|---|
| Containerized API | http://localhost:5080 |
| Health endpoint | http://localhost:5080/health |
| Tickets endpoint | http://localhost:5080/api/tickets |
| Swagger UI | http://localhost:5080/swagger |

Swagger is enabled in the Development environment.

### Stop the application

```bash
docker compose down
```

To remove the database volume and reset the local database:

```bash
docker compose down -v
```

**Warning:** `docker compose down -v` permanently removes the Compose database volume and its local data.

### Environment configuration

Configuration is supplied at runtime through environment variables. The connection string uses `ConnectionStrings__Default`, where double underscores represent nested configuration keys in ASP.NET Core.

The SQL Server password is supplied through `MSSQL_SA_PASSWORD`. The Docker image does not copy the local `.env` file into the image.

Use `.env.example` as the template for local configuration. Do not commit real credentials or production secrets.

### Database migrations and seeding

In Development, the API applies Entity Framework Core migrations and seeds demo data on startup. This allows a fresh local Docker database to be initialized automatically.

For production deployments, database migrations should be run as a controlled deployment step rather than automatically by every application instance. Production secrets should be supplied through the deployment environment or a secrets-management solution.

### Frontend with the containerized API

To use the containerized API from the frontend, configure:

```dotenv
VITE_API_PROXY_TARGET=http://localhost:5080
```

Then run the web application using the existing frontend setup:

```bash
npm run web
```

Ensure the API container is running before starting the frontend.

## Tests

Run the project tests using:

```bash
npm test
```

This runs the .NET tests and the frontend Vitest tests, according to the repository scripts.

Other useful commands:

```bash
npm run build
npm run lint
npm run typecheck
npm run format
```

Check the output of each command for failures. Tests and build results should be reported based on the latest run.

### Docker verification

The following Docker checks were performed locally:

| Check | Result |
|---|---|
| `GET /health` | Returned `Healthy` |
| `GET /api/tickets` | Returned HTTP `200 OK` with ticket data |
| `docker compose exec api whoami` | Returned `app`, confirming non-root execution |
| Database initialization | Logs confirmed 5 categories, 6 customers, 6 agents, and 40 tickets were seeded |

Additional assignment verification:

- [ ] Run `docker compose down -v` followed by `docker compose up --build` and verify the fresh database initializes correctly.
- [ ] Modify a `.cs` file and rebuild to verify that the Docker restore layer is cached.
- [ ] Record the actual runtime image size and compare it with the SDK image size.

## API at a Glance

| Endpoint | Description |
|---|---|
| `GET /health` | Checks database connectivity and returns the health status |
| `GET /api/tickets` | Returns a paginated ticket list and supports the implemented query filters |
| `GET /api/tickets/{id}` | Retrieves a ticket by ID |
| `POST /api/tickets` | Creates a ticket |
| `PATCH /api/tickets/{id}/status` | Changes a ticket's status |
| `PATCH /api/tickets/{id}/assignment` | Changes a ticket's assignment |
| `GET /api/customers` | Retrieves customer reference data |
| `GET /api/agents` | Retrieves agent reference data |
| `GET /api/categories` | Retrieves category reference data |

The ticket-list query supports parameters such as `search`, `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId`, `unassignedOnly`, `sortBy`, and `sortDirection`. Confirm the implemented behavior in the API before submission.

API errors use `ProblemDetails` responses. Expected error categories include validation errors (`400`), missing resources (`404`), and conflicts or business-rule violations (`409`).

## Seed Data

The demo dataset contains:

- 5 categories
- 6 customers, including 3 Premium customers
- 6 agents, including an inactive agent and an agent at their assignment limit
- 40 tickets across different statuses, priorities, and SLA states

## Design Decisions

### Docker

- **Multi-stage build:** The SDK is used only during build and publish. The runtime image contains the published application and ASP.NET Core runtime.
- **Restore layer caching:** Project files and shared build configuration are copied before the application source, allowing Docker to reuse the restore layer when only source files change.
- **Non-root execution:** The API runs as the `app` user rather than `root`.
- **Runtime configuration:** Connection strings and passwords are supplied through environment variables instead of being baked into the image.
- **Service-name networking:** The API connects to SQL Server using `sqlserver` as the hostname inside the Compose network.
- **Health endpoint:** `/health` checks database connectivity and returns a failure status if the database cannot be reached.

### Other design decisions

Document the actual choices made for Tasks 1–3 here. Explain the defect's root cause, why the selected filtering approach was used, how ticket triage and escalation work, how SLA status is calculated, and how authentication is validated.

## Defect Root Cause

**Complete this section with the confirmed root cause from Task 1.**

Explain what caused the ticket-list defect, which code was changed, and how the regression test proves the fix. Do not describe the symptom alone; identify the underlying cause.

## Development Login Credentials

**Development login:** `[Add the actual development username or email]`

**Password:** `[Add the actual development password]`

Only provide credentials that are configured and verified in the submitted application. Do not include production credentials or real secrets in this README.

If authentication was not completed, state that clearly instead of providing invented credentials.

## AI Tool Usage

AI tools were used as development assistance for tasks such as understanding requirements, investigating defects, reviewing implementation approaches, generating or refining code, and troubleshooting errors.

All AI-assisted code was reviewed and tested against the project requirements. The submitted implementation should be understood by the developer and should be explainable during a technical review.

**Tools used:** `[List the AI tools actually used, such as ChatGPT, Claude, or GitHub Copilot.]`

## Known Limitations

Update this list to reflect the actual implementation.

- Registration and password reset are outside the assignment scope.
- Roles, OAuth/SSO, refresh tokens, and multi-factor authentication are not included.
- Notifications and caching are not included.
- CI/CD and automated deployment pipelines are not included.
- Production HTTPS termination, production secrets management, and deployment-specific migration orchestration must be configured for a real deployment.
- Any unfinished or partially implemented assignment features should be listed here explicitly.

## Troubleshooting

| Problem | Suggested fix |
|---|---|
| API cannot reach SQL Server | Start SQL Server with `npm run db:up` for local development, or check `docker compose ps` when using the containerized API. |
| SQL Server is still starting | Wait for its health check to pass before testing the API. |
| Port 1433 is already in use | Stop the conflicting service or change the SQL Server host-port mapping and connection configuration. |
| Browser reports an HTTPS certificate warning | Run `dotnet dev-certs https --trust`, or use the frontend proxy. |
| Frontend cannot load tickets | Check that the API is running and that `VITE_API_PROXY_TARGET` points to the correct API URL. |
| Docker build fails | Check the Dockerfile path, Docker Desktop status, and build output. |
| Database needs a clean reset | Run `docker compose down -v`, remembering that this deletes the local database volume. |

## Repository and Submission

**GitHub fork:** `[Add your fork URL]`

**Development credentials:** `[Provide verified credentials separately if required]`

**Time spent:** `[Add your approximate time spent on Tasks 1–4]`

See `APPROACH.md` for the implementation approach, decisions, testing, and remaining limitations for each assignment task.
