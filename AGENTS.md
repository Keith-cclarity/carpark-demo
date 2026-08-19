# Repository Instructions for Coding Agents

This file gives coding agents accurate context about the repository so they can
work safely and consistently without inventing architecture, commands, or
features that are not present.

---

## Project Overview

**Smart Parking Navigator** is a workshop application that helps users find
available HDB car parks near a destination in Singapore. The starter provides
an empty but buildable scaffold; application features are not yet implemented.

---

## Repository Structure

```text
/
├── data/
│   ├── carpark-availability.http   # Sample HTTP request against data.gov.sg
│   ├── CarparkAvailability.json    # Representative API response snapshot
│   └── HDBCarparkInformation.csv  # Static HDB car park reference data
├── docs/                           # Workshop step-by-step guides
├── src/
│   ├── CarparkAvailability.ApiApp/        # ASP.NET Core Web API (server-side)
│   ├── CarparkAvailability.AppHost/       # .NET Aspire orchestration host
│   ├── CarparkAvailability.ServiceDefaults/  # Shared Aspire service defaults
│   └── CarparkAvailability.WebApp/        # Blazor Server frontend
├── CarparkAvailability.slnx        # Solution file
├── Directory.Packages.props        # Central NuGet package versions
├── global.json                     # .NET SDK version pin
├── AGENTS.md                       # This file
├── IDEATION.md                     # Product concept and data notes
└── README.md
```

### Project Responsibilities

| Project | Role |
|---------|------|
| **ApiApp** | ASP.NET Core Web API. Fetches live car park availability from data.gov.sg (server-side only) and joins it with HDB static data. Exposes endpoints consumed by WebApp. |
| **WebApp** | Blazor Server frontend. Renders the map and parking UI. Calls ApiApp through .NET Aspire service discovery. Renders Google Maps JavaScript in the browser. |
| **AppHost** | .NET Aspire `DistributedApplication` that wires ApiApp and WebApp together, injects secrets, and runs both in development. |
| **ServiceDefaults** | Shared extension methods for OpenTelemetry, resilience, and health checks, applied by `AddServiceDefaults()` in both ApiApp and WebApp. |

---

## Technology Stack

- **.NET 10** (SDK `10.0.100`, pinned in `global.json`)
- **.NET Aspire 13.4** (`Aspire.AppHost.Sdk`)
- **ASP.NET Core** – minimal API style in ApiApp
- **Blazor Server** with Interactive Server render mode in WebApp
- **OpenTelemetry** – configured through ServiceDefaults
- **Central Package Management** – all NuGet versions live in
  `Directory.Packages.props`; individual project files do not specify versions

---

## Data and API Contract

| File | Description |
|------|-------------|
| `data/HDBCarparkInformation.csv` | Static HDB car park reference data (car park number, address, coordinates in SVY21, lot types, restrictions). Copied into ApiApp's output directory. |
| `data/CarparkAvailability.json` | Representative snapshot of the data.gov.sg Car Park Availability API response. Use as the source of truth for the response schema. |
| `data/carpark-availability.http` | Sample HTTP request showing the correct URL and headers for the data.gov.sg endpoint. |

**Important:** HDB coordinates use the **SVY21** system. Convert to **WGS84**
latitude/longitude before passing to any map API.

---

## Secrets and Configuration

AppHost reads secrets from `dotnet user-secrets` and injects them as
environment variables:

| Secret name | Environment variable | Consumer |
|-------------|----------------------|----------|
| `GoogleMaps:ApiKey` | `GoogleMaps__ApiKey` | WebApp |
| `DataGovSg:ApiKey` | `DataGovSg__ApiKey` | ApiApp |

Set secrets in AppHost before running:

```bash
dotnet user-secrets --project src/CarparkAvailability.AppHost set "GoogleMaps:ApiKey" "<key>"
dotnet user-secrets --project src/CarparkAvailability.AppHost set "DataGovSg:ApiKey" "<key>"
```

See `docs/google-maps-api-key.md` and `docs/data-gov-sg-api-key.md` for
key-acquisition instructions.

---

## Commands

All commands run from the repository root unless stated otherwise.

### Restore and build

```bash
dotnet build CarparkAvailability.slnx
```

### Run the application

```bash
dotnet run --project src/CarparkAvailability.AppHost
```

The Aspire dashboard URL is printed on startup. AppHost starts ApiApp and
WebApp automatically.

### Validate the API scaffold

```bash
curl -s http://localhost:<port>/api
```

Replace `<port>` with the port shown in the Aspire dashboard or the ApiApp
launch profile.

---

## General Guidelines

- **Do not invent commands.** Only use commands listed here or present in
  existing project files.
- **Do not invent project structure.** Add files only inside the four existing
  projects or `data/` and `docs/`.
- **Do not fabricate application features.** The starter scaffold has no
  parking logic yet. Implement features only when a requirement document
  (`PRD.md` or `TRD.md`) is present.
- **Central Package Management.** Add new NuGet packages to
  `Directory.Packages.props` and reference them without a version in the
  project file.
- **ServiceDefaults pattern.** Both ApiApp and WebApp call
  `builder.AddServiceDefaults()`. Extend shared cross-cutting concerns there,
  not inline in each project.

---

## Service Boundary Rules

- **Google Maps JavaScript API** must only be called from the **browser**
  (WebApp Razor components). Do not call it from ApiApp or from server-side
  Blazor code.
- **data.gov.sg API** must only be called from **ApiApp** (server-side). Do
  not call data.gov.sg directly from any Blazor component, JavaScript, or
  browser context.

---

## Security Guardrails

- **Never commit credentials.** API keys, connection strings, and tokens must
  be stored in `dotnet user-secrets` or environment variables. Do not put them
  in `appsettings.json`, source files, or any tracked file.
- Treat `data/CarparkAvailability.json` as a sanitized example. Do not replace
  it with a live API response that contains real credentials or PII.
- If a secret is accidentally staged, remove it before committing and rotate
  the key immediately.

---

## Testing

No tests exist yet. When tests are added they must:

- Live in a project named `*.Tests` inside `src/` and be included in
  `CarparkAvailability.slnx`.
- Follow the xUnit convention already implied by the .NET template.
- Be runnable with `dotnet test CarparkAvailability.slnx`.
- Include contract tests that validate representative live responses against
  `data/CarparkAvailability.json`.

Do not invent a test runner or test command that is not yet present.

---

## Documentation

- `IDEATION.md` — product concept, data notes, and recommended priorities.
  Read before implementing features.
- `docs/` — workshop step-by-step guides. Do not modify these files during
  feature implementation.
- `PRD.md` and `TRD.md` — will be created in a later workshop step. All
  feature implementation and test expectations must follow those documents once
  they exist. Do not implement features before they are present.

---

## Commit and Pull-Request Guidelines

### Commits

- Use the **Conventional Commits** format:
  `<type>(<optional scope>): <short summary>`
- Common types: `feat`, `fix`, `docs`, `chore`, `test`, `refactor`.
- Keep the subject line under 72 characters.
- Use the body to explain *why*, not *what*, when the reason is not obvious.
- One logical change per commit.

### Pull Requests

- Title follows the same Conventional Commits format as the commit subject.
- Description must include: what changed, why, and how to verify.
- Link the related issue with `Closes #<number>`.
- Ensure `dotnet build CarparkAvailability.slnx` passes before requesting review.
- Do not merge without at least one approving review.
