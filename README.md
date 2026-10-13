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
2. Writes `resume_profile.json` from the `RESUME_PROFILE_JSON` GitHub Actions secret.
3. Supplies `GEMINI_API_KEY` and `JOOBLE_API_KEY` to `dotnet run -- cron`.
4. Commits changes to `wwwroot/latest_matches.json` when the output differs.

This is **GitHub Actions-based application automation**, not a Kubernetes deployment pipeline.

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
- Deployment manifests, live service evidence and failure-injection results should be documented only after implementation and validation.

## Relationship to EuroJobAgent

[EuroJobAgent](https://github.com/BharadwajGanti-creator/EuroJobAgent) is a separate evolution with cross-run tracking, sponsorship-specific search behaviour, drafting and email notifications, as well as documented personal Docker/Kubernetes/AKS exercises.
