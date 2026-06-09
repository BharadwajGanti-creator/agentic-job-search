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
            You score how well each job fits a candidate.
            For each job return: its index, fitScore (0-100), a one-line reason, and the top missing skill ("none" if none).
            Weigh skills overlap, seniority fit, and target relevance.
            Be strict: a Director/Principal/Manager role is a poor fit for a Mid candidate;
            an SRE/AI/DevOps role is a poor fit for a .NET backend candidate.
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
            sb.AppendLine($"{i}. {jobs[i].Title} @ {jobs[i].Company} ({jobs[i].Location})");
        return ParseJson<Score[]>(await RunAndClean(_matchAgent, sb.ToString()));
    }

    private static async Task<string> RunAndClean(AIAgent agent, string prompt)
    {
        string raw = (await agent.RunAsync(prompt)).Text.Trim();
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