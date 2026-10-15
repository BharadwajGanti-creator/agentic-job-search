# Agentic Job Search

A personal C#/.NET 10 application for finding and ranking backend/software engineering vacancies in Dublin. It queries job postings through Jooble, uses an LLM-based matcher for fit scoring, and supports scheduled runs, a web interface, and evaluation against manually labelled job examples.

This is an independent **software engineering/automation project**, not an enterprise production service or a claim of professional SRE experience. It is separate from [EuroJobAgent](https://github.com/BharadwajGanti-creator/EuroJobAgent), which targets visa-sponsorship searches and adds status persistence, drafting and email delivery.

## How it works

```text
resume_profile.json
      |
      v
JobMatcher (query planning) --> JoobleJobSource (vacancy search)
      |                                |
      +------------> SearchService <---+
                         |
                         v
                  Match/fit scoring
                         |
                +--------+---------+
                |                  |
       CLI cron → JSON output    Web API → dashboard/static files
                |
       wwwroot/latest_matches.json
```

[Program.cs](Program.cs) defines three modes:

- `dotnet run -- cron` searches and ranks postings and writes `wwwroot/latest_matches.json`.
- `dotnet run -- dump` fetches an unlabelled batch for constructing a manually reviewed evaluation set.
- `dotnet run -- eval` scores the matcher against `golden_set.json` and reports accuracy, precision and recall.
- `dotnet run` starts the ASP.NET Core web application, serves static files and exposes `GET /health` and `POST /api/search`.

The application is focused on backend/SDE opportunities in Dublin; see `Program.cs` for the current hardcoded target and location.

## Setup and local use

Prerequisites: .NET 10 SDK and credentials for the external job/LLM APIs referenced by the application.

1. Create a local `resume_profile.json` with your actual candidate information in the schema expected by the application. Keep personal profile data and credentials out of public commits.
2. Configure API credentials locally using the environment variables expected by the source (for example `GEMINI_API_KEY` and `JOOBLE_API_KEY`).
3. Build and run:

```bash
dotnet build
dotnet run -- cron
dotnet run -- eval
dotnet run
```

Run the modes separately. The evaluation mode requires a populated, manually labelled `golden_set.json`. Check the application's console output for the listening address when running the web mode.

## Scheduled execution

[`.github/workflows/daily.yml`](.github/workflows/daily.yml) schedules execution at **06:00 UTC** and permits manual runs. The workflow:

1. Checks out the repository and installs .NET 10.
2. Authenticates to Azure through GitHub OIDC; no Azure client secret is stored in the workflow.
3. Reads `ResumeProfileJson`, `GeminiApiKey`, and `JoobleApiKey` from Azure Key Vault.
4. Materialises `resume_profile.json` for the application process and exports the API keys only for the run.
5. Executes `dotnet run -- cron`.
6. Commits changes to `wwwroot/latest_matches.json` when the output differs.

Application secrets are therefore sourced from Azure Key Vault rather than duplicated as GitHub Actions application secrets.

## Platform deployment and CI/CD

The repository also contains the infrastructure and deployment work used for the current cloud/platform engineering track:

- `infra/` contains Bicep that reconciles the existing Azure resource group, ACR and AKS cluster and provisions the Key Vault configuration used by the project.
- `k8s/agentic-job-search.yaml` defines a namespace-scoped Deployment and ClusterIP Service with readiness/liveness probes on `GET /health`, resource requests/limits, and `envFrom.secretRef`.
- `.github/workflows/oidc-smoke.yml` demonstrates GitHub-to-Azure OIDC authentication and deliberately verifies that authentication fails when `id-token: write` is omitted.
- `.github/workflows/aks-authorization-smoke.yml` checks the Kubernetes authorization boundary: the deployment identity can perform the intended write operation in the `agentic-job-search` namespace and is denied the equivalent operation in `default`.
- `.github/workflows/ci-cd.yml` restores/builds the .NET application, authenticates with OIDC, requires AKS to be intentionally running, builds and pushes an immutable `GITHUB_SHA`-tagged image to ACR, reads application secrets from Key Vault, creates/updates the Kubernetes Secret, validates and applies the manifest, waits for rollout, verifies the deployed image tag, and checks `/health` through the ClusterIP Service.
- The deployment workflow deliberately refuses to start a stopped AKS cluster automatically; this keeps compute-cost activation an explicit operator decision.

The design and trade-offs around health probes and runtime secret materialisation are recorded in [ADR-003](docs/adr-003-web-mode-health-and-secrets.md).

## Evaluation

[Eval.cs](Eval.cs) compares scored results with manually labelled examples and prints a confusion-matrix-derived accuracy, precision and recall at a fit-score threshold of **60**.

The approximately **84% accuracy / 75% recall** figures described in the project's LinkedIn entry are historical reported evaluation results; this README does not claim that a fresh evaluation run was performed as part of this documentation change. Results depend on the labels, sample and current model behaviour.

## Container

The root [Dockerfile](Dockerfile) performs a multi-stage .NET 10 build and exposes the application on port **8080**. [entrypoint.sh](entrypoint.sh) can write `resume_profile.json` from the `RESUME_PROFILE_JSON` environment variable before starting the app.

```bash
docker build -t agentic-job-search:local .
docker run --rm -p 8080:8080 --env-file .env agentic-job-search:local
```

Only use a local `.env` file containing the variables your configuration requires; do not commit it or expose secrets in screenshots. This section documents the container configuration, not a claim that it was validated during this README update.

## Limitations and engineering follow-ups

- The target role and location are configured directly in `Program.cs`, limiting reuse without code edits.
- Scheduled runs overwrite `latest_matches.json`; the file is a latest-result snapshot rather than a historical job tracker.
- Job search depends on external APIs and LLM responses, so failure handling and ranking quality require ongoing verification.
- Evaluation metrics should be accompanied by a reproducible labelled data set and the date/model used for each reported run.
- The current AKS environment is deliberately small personal-subscription infrastructure used for hands-on engineering practice, not a claim of years of production Kubernetes/SRE operation.
- The deployment pipeline currently materialises a Kubernetes Secret from Azure Key Vault at deploy time; this is intentionally documented as a trade-off rather than equivalent to a native external-secrets integration.

## Relationship to EuroJobAgent

[EuroJobAgent](https://github.com/BharadwajGanti-creator/EuroJobAgent) is a separate evolution with cross-run tracking, sponsorship-specific search behaviour, drafting and email notifications, as well as documented personal Docker/Kubernetes/AKS exercises.
