using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

// ---------- CONSOLE MODES (Phase 4 tooling) ----------
if (args.Contains("eval"))
{
    await Eval.RunAsync();
    return;
}

var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var profile = JsonSerializer.Deserialize<ResumeProfile>(
    File.ReadAllText("resume_profile.json"), jsonOpts)!;

const string target = "Backend / Software Engineer (SDE) roles in Dublin, Ireland. " +
                      "Open to .NET, C#, and backend positions at mid level.";
const string location = "Dublin, Ireland";

var matcher = new JobMatcher();
IJobSource jobSource = new JoobleJobSource();

if (args.Contains("dump"))
{
    string[] dq = await matcher.PlanQueriesAsync(profile, target);
    var seen = new HashSet<string>();
    var jobs = new List<Job>();
    foreach (var q in dq)
        foreach (var job in await jobSource.SearchAsync(q, location))
            if (seen.Add(job.Link)) jobs.Add(job);

    File.WriteAllText("jobs_dump.json", JsonSerializer.Serialize(
        jobs.Select(j => new { j.Title, j.Company, j.Location, j.Snippet, relevant = false }),
        new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Wrote {jobs.Count} jobs to jobs_dump.json");
    return;
}

if (args.Contains("cron"))
{
    var trace = await SearchService.RunAsync(profile, target, matcher, jobSource, location);
    var payload = new
    {
        generatedAtUtc = DateTime.UtcNow.ToString("u"),
        trace.PlannedQueries,
        trace.Fetched,
        trace.Unique,
        trace.Ranked
    };
    Directory.CreateDirectory("wwwroot");
    var camel = new JsonSerializerOptions
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    File.WriteAllText("wwwroot/latest_matches.json", JsonSerializer.Serialize(payload, camel));
    Console.WriteLine($"Wrote latest_matches.json — {trace.Ranked.Count} ranked, {trace.Unique} unique");
    return;
}

// ---------- WEB MODE (Phase 5) ----------
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();   // serves wwwroot/index.html at "/"
app.UseStaticFiles();

app.MapPost("/api/search", async () =>
{
    try
    {
        var trace = await SearchService.RunAsync(profile, target, matcher, jobSource, location);
        return Results.Json(trace);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 500);
    }
});

app.Run();