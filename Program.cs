using System.Text.Json;

if (args.Length > 0 && args[0] == "eval")
{
    await Eval.RunAsync();
    return;
}

// ----- LIVE SEARCH MODE -----
var profile = JsonSerializer.Deserialize<ResumeProfile>(
    File.ReadAllText("resume_profile.json"),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

string target = "Backend / Software Engineer (SDE) roles in Dublin, Ireland. " +
                "Open to .NET, C#, and backend positions at mid level.";

var matcher = new JobMatcher();
IJobSource jobSource = new JoobleJobSource();

string[] queries = await matcher.PlanQueriesAsync(profile, target);
Console.WriteLine($"Planned queries: {string.Join(" | ", queries)}\n");

var seen = new HashSet<string>();
var jobs = new List<Job>();
foreach (var q in queries)
    foreach (var job in await jobSource.SearchAsync(q, "Dublin, Ireland"))
        if (seen.Add(job.Link)) jobs.Add(job);

Console.WriteLine($"Fetched {jobs.Count} unique jobs.\n");

var scores = await matcher.ScoreJobsAsync(profile, target, jobs);

Console.WriteLine("----- RANKED MATCHES -----\n");
foreach (var s in scores.OrderByDescending(x => x.FitScore))
{
    if (s.Index < 0 || s.Index >= jobs.Count) continue;
    var job = jobs[s.Index];
    Console.WriteLine($"[{s.FitScore,3}] {job.Title} @ {job.Company}");
    Console.WriteLine($"      Why:     {s.Reason}");
    Console.WriteLine($"      Missing: {s.Missing}");
    Console.WriteLine($"      Link:    {job.Link}\n");
}