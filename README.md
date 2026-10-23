# C# Starter Service

A minimal, independent starter repository for Session 01 of the Software Engineering Lab.
The task is to run the same health endpoint on every team member's computer.
No database, authentication, or external service is required.

## Requirements

- .NET SDK 10.0.x (includes ASP.NET Core). A runtime alone is not enough.
- Git to clone and push the repository.
- Internet access for the first dependency download.
- Docker is optional for running this service; use it only if you choose the Docker route below.

## API contract

| Item | Value |
| --- | --- |
| Method | `GET` |
| Path | `/health` |
| HTTP status | `200 OK` |
| Content type | `application/json` (an optional charset is allowed) |
| JSON body | `{"status":"ok"}` |
| Local URL | `http://127.0.0.1:8080/health` |

JSON whitespace is insignificant. Header order and framework-specific headers may differ
between languages. Error body formats are not part of the shared contract.
`POST /health` returns 405; an unknown route returns 404.
This endpoint checks that the application is responding, not the health of future dependencies.

## Run locally

Open a terminal in this repository's root, then run:

```bash
dotnet --info
dotnet restore
dotnet run --no-launch-profile
```

Keep that terminal open. Stop the server with `Ctrl+C`.

Build without keeping a server running:

```bash
dotnet build
```

## Check the service

In a second terminal on Windows PowerShell:

```powershell
curl.exe -i http://127.0.0.1:8080/health
```

On macOS or Linux:

```bash
curl -i http://127.0.0.1:8080/health
```

Expect HTTP 200, JSON content type, and `{"status":"ok"}`.
You can also open the URL in your browser.

For an optional automated contract check, install Python 3.10+ and run:

```bash
python scripts/verify_health.py
```

On Windows, use `py scripts/verify_health.py` if `python` is not available;
on macOS/Linux use `python3`. No extra Python packages are needed for this check.
The server must already be running. The script checks GET, the rejected POST method,
and an unknown route. It exits with a nonzero status on failure.

## Run with Docker

From the repository root:

```bash
docker build -t starter-csharp .
docker run --rm -p 127.0.0.1:8080:8080 starter-csharp
```

The container listens on all container interfaces; the published port is restricted to
localhost on your machine. Stop the foreground container with `Ctrl+C`.
The Docker image is for a teaching starter; registry tags receive updates and are not digest-pinned.

## Port conflicts

Run only one starter on port 8080 at a time. Stop the previous server first.
To use port 8081 with Docker, change the mapping to `127.0.0.1:8081:8080`.
Then check `http://127.0.0.1:8081/health`.

Local port override (PowerShell):

```powershell
dotnet run --no-launch-profile --urls http://127.0.0.1:8081
```

## Files to know

- `Program.cs`: health endpoint implementation.
- `Dockerfile`: optional container build and startup.
- `.gitignore`: excludes generated files and local settings.
- `scripts/verify_health.py`: optional black-box API verification.

## Session 05 — Testing

This session adds a second endpoint, a test suite, and a contract test that runs
against the **neighbouring team's running service** rather than against a mock.

### The orders endpoint

`POST /orders` takes `{"item": "...", "quantity": N}` and answers `201` with the
created order, or `400` with `{"error": "validation_failed", "field": "..."}`.
`GET /orders/{id}` answers `200` with the order or `404` with
`{"error": "not_found"}`. The full contract is in `openapi.yaml`, and
`openapi.json` is the same document in the form the tests read.

The validation bounds are part of that contract: `item` is 1 to 64 characters
after trimming, `quantity` is an integer from 1 to 1000.

### Install the test dependencies

The first `dotnet test` restores xUnit, Moq and coverlet:

```bash
dotnet restore tests/StarterService.Tests
```

### Run the tests

```bash
make test        # or: dotnet test tests/StarterService.Tests
```

The contract tests report as **skipped** until you point them at a neighbour —
see below. A skipped contract test is not a passing one.

### Coverage

```bash
make coverage    # or: dotnet test --collect:"XPlat Code Coverage"
```

The report lands in the Cobertura file under `tests/StarterService.Tests/TestResults/`. The number on its own is not the point: your
report has to explain what is *not* covered and why.

### Put your sandbox on the network

Your neighbouring team cannot reach `127.0.0.1` on your machine. Bind every
interface instead:

```bash
make run        # listens on 0.0.0.0:8080
# or in Docker:
make sandbox
```

Then find your address and give it to them:

```bash
ip addr                    # Linux
ipconfig                   # Windows
ipconfig getifaddr en0     # macOS
```

They will use `http://<your-ip>:8080`. If they cannot reach it, the usual
causes are a desktop firewall blocking the port and the service still listening
on localhost.

### Run the contract test against your neighbour

```bash
make contract-test NEIGHBOR_BASE_URL=http://10.0.0.42:8080
```

It sends real HTTP to their service and checks every response against
`openapi.json`. Each call prints a timestamp — copy that output into your
report, it is the evidence that the test ran against a real address.

Two `TODO(team)` markers in the contract test are the only things you should
need to change: the path, and the request body, if your neighbour's service
differs from the shared contract.

### If the lab network blocks machine-to-machine traffic

Run your neighbour's service on your own machine instead. The test stays real —
real HTTP against a real implementation — only the address changes:

```bash
NEIGHBOR_REPO=https://github.com/AUT-SE1/starter-go.git \
  docker compose -f docker-compose.neighbor.yml up --build -d
make contract-test NEIGHBOR_BASE_URL=http://127.0.0.1:8081
```

`NEIGHBOR_REPO` may also be a local path to their cloned repository.

### On Windows

`make` is not installed on Windows by default, so every `make` command above has
a plain equivalent. Run these from the repository root.

| Instead of | Run |
|---|---|
| `make run` | `dotnet run --urls http://0.0.0.0:8080` |
| `make test` | `dotnet test tests\StarterService.Tests` |
| `make coverage` | `dotnet test tests\StarterService.Tests --collect:"XPlat Code Coverage"` |

**The contract test needs the address in an environment variable**, and the
`VAR=value command` form in the examples above is Unix shell syntax that does
not work in Windows. Set the variable on its own line first.

In Command Prompt:

```bat
set NEIGHBOR_BASE_URL=http://10.0.0.42:8080
dotnet test tests\StarterService.Tests --filter FullyQualifiedName~NeighborContractTests
```

In PowerShell:

```powershell
$env:NEIGHBOR_BASE_URL = "http://10.0.0.42:8080"
dotnet test tests\StarterService.Tests --filter FullyQualifiedName~NeighborContractTests
```

The variable lasts only as long as that terminal window, so set it again in a
new one.

**Find the address to give your neighbour** with `ipconfig`, and read the IPv4
address of the adapter you are actually connected through — usually "Wireless
LAN adapter Wi-Fi".

**If they still cannot reach you**, Windows Defender Firewall is the usual
reason. In a PowerShell window opened as Administrator:

```powershell
New-NetFirewallRule -DisplayName "SE Lab 8080" -Direction Inbound `
  -LocalPort 8080 -Protocol TCP -Action Allow
```

Remove it after the session with
`Remove-NetFirewallRule -DisplayName "SE Lab 8080"`.

### Files added for this session

- `Orders.cs`: order validation and the in-memory store — the logic under unit test.
- `tests/StarterService.Tests/OrdersTests.cs`: unit tests.
- `tests/StarterService.Tests/OrderApiTests.cs`: integration tests through `WebApplicationFactory`.
- `tests/StarterService.Tests/Contract/NeighborContractTests.cs`: the contract test.
- `tests/StarterService.Tests/Contract/OpenApiCheck.cs`: a small OpenAPI schema check.
- `openapi.yaml` / `openapi.json`: the shared contract.
- `docs/test-matrix.md`: the matrix to fill in during the session.
- `Makefile`: `run`, `test`, `coverage`, `contract-test`, `sandbox`.
- `docker-compose.yml`: your sandbox. `docker-compose.neighbor.yml`: the fallback.

## Publish this starter as a new repository

Create an **empty** GitHub repository named `starter-csharp`. Do not initialize it with a README,
license, or gitignore. From this extracted folder (which has no Git history), run:

```bash
git init -b main
git add .
git commit -m "Add minimal health service starter"
git remote add origin https://github.com/YOUR_ORG/starter-csharp.git
git push -u origin main
```

Replace `YOUR_ORG` with your GitHub organization or account. Configure your own Git name
and email first. These commands are for a new repository, not an existing working copy.
After publishing, the repository can optionally be marked as a template in GitHub settings.

## Session 01 completion checklist

- [ ] Three team members and the chosen language are registered.
- [ ] The unchanged starter code has been pushed to the team's `main` branch.
- [ ] Every member can clone the team repository.
- [ ] Every member gets the successful `/health` response locally.
- [ ] The report includes the team repository URL and evidence of each member's successful run.

## Troubleshooting

- **Connection refused:** start the service and check its terminal output and port.
- **Address already in use:** stop the other service or use the override above.
- **404 on `/`:** expected; request `/health` instead.
- **Dependency download failed:** check network/proxy access to the package registry.
- **Command not found:** install the SDK/tool listed above and reopen the terminal.

## Official reference

[C# web service documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0)
