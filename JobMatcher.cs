using GenerativeAI.Microsoft;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

public class JobMatcher
{
    private readonly AIAgent _queryAgent;
    private readonly AIAgent _matchAgent;

    public JobMatcher()
    {
        string geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException("GEMINI_API_KEY is not set.");
        IChatClient chat = new GenerativeAIChatClient(geminiKey, "gemini-2.5-flash-lite");

        _queryAgent = chat.AsAIAgent(
            name: "QueryPlanner",
            instructions: """
            You plan job-search queries. Given a candidate profile and target,
            output 3 short search queries likely to surface relevant jobs.
            Return ONLY a JSON array of strings, no markdown.
            """);

        _matchAgent = chat.AsAIAgent(
    name: "Matcher",
    instructions: """
    You score how well each job fits the candidate: a mid-level backend software engineer.
    For each job return: its index, fitScore (0-100), a one-line reason, and the top missing skill ("none" if none).

    RELEVANT (score 60-100): individual-contributor roles that build backend
    application software, services, APIs, or data-processing systems, at mid or
    senior level. Programming language does NOT matter — Java, Python, Go, C#/.NET
    backend roles are all good fits. NEVER penalize a role for not using .NET.

    NOT RELEVANT (score 0-40), no matter how much "develop / engineer / build / code"
    language the description contains:
    - Management or above-senior IC: Manager, Lead, Principal, Staff, Director, Head, VP.
    - Test/quality roles: QA, SDET, "in Test", test automation.
    - Infrastructure/operations: SRE, DevOps, Network, Platform/Linux infra, Cloud ops.
    - Specialist roles whose core discipline is not general backend engineering:
      AI/ML modeling, Database/DBA engineering, hardware/applications engineering.

    Judge by ROLE TYPE and SENIORITY, not by buzzwords. The words "develop", "code",
    and "engineer" appear in every posting and mean nothing on their own.

    Return ONLY a JSON array, no markdown:
    [{"index":0,"fitScore":82,"reason":"...","missing":"..."}]
    """);
    }

    public async Task<string[]> PlanQueriesAsync(ResumeProfile profile, string target) =>
        ParseJson<string[]>(await RunAndClean(_queryAgent, $"""
        Skills: {string.Join(", ", profile.Skills)}
        Seniority: {profile.SeniorityLevel}
        Target: {target}
        """));

    public async Task<Score[]> ScoreJobsAsync(ResumeProfile profile, string target, List<Job> jobs)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Candidate skills: {string.Join(", ", profile.Skills)}");
        sb.AppendLine($"Seniority: {profile.SeniorityLevel}");
        sb.AppendLine($"Target: {target}\n");
        sb.AppendLine("Jobs:");
        for (int i = 0; i < jobs.Count; i++)
{
    sb.AppendLine($"{i}. {jobs[i].Title} @ {jobs[i].Company} ({jobs[i].Location})");
    if (!string.IsNullOrWhiteSpace(jobs[i].Snippet))
        sb.AppendLine($"   Description: {jobs[i].Snippet}");
}
        var scores = ParseJson<Score[]>(await RunAndClean(_matchAgent, sb.ToString()));

var returned = scores.Select(s => s.Index).ToHashSet();
var missing = Enumerable.Range(0, jobs.Count).Where(i => !returned.Contains(i)).ToList();
if (missing.Count > 0)
    throw new InvalidOperationException(
        $"Matcher dropped {missing.Count} job(s): indices [{string.Join(", ", missing)}]. " +
        "Scores incomplete — don't trust the eval.");
return scores;
    }

    private static async Task<string> RunAndClean(AIAgent agent, string prompt)
{
    var runOptions = new ChatClientAgentRunOptions(new ChatOptions { Temperature = 0f });
    string raw = (await agent.RunAsync(prompt, options: runOptions)).Text.Trim();
    if (raw.StartsWith("```"))
    {
        int nl = raw.IndexOf('\n');
        raw = raw[(nl + 1)..];
        if (raw.EndsWith("```")) raw = raw[..^3];
        raw = raw.Trim();
    }
    return raw;
}

    private static T ParseJson<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
}