# ADR-003: Web-mode health endpoint and runtime secret materialisation

## Status

Accepted

## Context

The agentic-job-search application requires a resume profile during startup.

`Program.cs` reads `resume_profile.json` from disk before the web application
starts.

The application also exposes `POST /api/search`, which performs real work
against external services including Gemini and Jooble.

Using `/api/search` as a Kubernetes readiness or liveness probe would therefore
be inappropriate because Kubernetes executes probes repeatedly. That would
cause unnecessary external API calls, consume quota, increase latency, and make
pod health depend on external services.

The application also requires:

- `GEMINI_API_KEY`
- `JOOBLE_API_KEY`
- `RESUME_PROFILE_JSON`

These values must not be committed to Git or baked into the container image.

## Decision

### Health endpoint

Expose a lightweight:

`GET /health`

endpoint.

Kubernetes readiness and liveness probes use `/health`.

`POST /api/search` is never used as a health probe.

The health endpoint verifies that the application process is running and able
to respond without making Gemini or Jooble API calls.

### Secret retrieval

Application secrets are stored in Azure Key Vault:

- `GeminiApiKey`
- `JoobleApiKey`
- `ResumeProfileJson`

The deployment pipeline authenticates to Azure using GitHub OIDC.

The pipeline reads the secrets from Azure Key Vault and creates or updates the
Kubernetes Secret:

`agentic-job-search-secrets`

The Key Vault names are explicitly mapped to application environment-variable
names:

- `GeminiApiKey` → `GEMINI_API_KEY`
- `JoobleApiKey` → `JOOBLE_API_KEY`
- `ResumeProfileJson` → `RESUME_PROFILE_JSON`

The Deployment references the Kubernetes Secret using `envFrom.secretRef`.

### Resume profile startup behaviour

The existing application expects `resume_profile.json` to exist on disk before
the .NET process starts.

The container entrypoint therefore performs:

`RESUME_PROFILE_JSON`
→ `entrypoint.sh`
→ `/app/resume_profile.json`
→ `dotnet JobSearchAgent.dll`

This preserves the application's startup behaviour without storing the resume
profile inside the container image.

## Consequences

### Positive

- Kubernetes probes do not generate real Gemini or Jooble traffic.
- External API availability does not determine basic pod health.
- API keys are not committed to Git.
- Secrets are not baked into Docker image layers.
- GitHub Actions authenticates to Azure without an Azure client secret.
- The same container image can be deployed with different runtime secrets.
- The deployed application retains its existing file-based resume-profile
  startup behaviour.

### Trade-offs

- Kubernetes receives copies of the secrets during deployment.
- Kubernetes Secret access must therefore remain protected through RBAC.
- The deployment pipeline now depends on Azure Key Vault availability and
  appropriate Key Vault permissions.
- Runtime secret rotation requires the Kubernetes Secret and pods to be
  refreshed before applications receive the new values.

## Alternatives considered

### Probe `/api/search`

Rejected because it invokes real external APIs and would make Kubernetes probes
perform business operations.

### Bake secrets into the Docker image

Rejected because container images may be stored, cached, scanned, or shared,
and secrets would become part of immutable image layers.

### Commit `resume_profile.json`

Rejected because the profile is treated as sensitive runtime configuration and
should not live in source control.

## Result

The application now separates process health from business operations and
separates application artifacts from runtime secrets.
