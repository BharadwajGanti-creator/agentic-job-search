using System.Text;
using System.Text.Json;

public record ResumeProfile(string[] Skills, double YearsOfExperience, string[] JobTitles,
                            string[] Domains, string SeniorityLevel, string[] Education, string Location);
public record Job(string Title, string Company, string Location, string Link, string Snippet);
public record Score(int Index, int FitScore, string Reason, string Missing);
public record GoldenItem(string Title, string Company, string Location, bool Relevant);

public interface IJobSource
{
    Task<List<Job>> SearchAsync(string keywords, string location);
}

public class JoobleJobSource : IJobSource
{
    private static readonly HttpClient Http = new();
    private readonly string _key = Environment.GetEnvironmentVariable("JOOBLE_API_KEY")
        ?? throw new InvalidOperationException("JOOBLE_API_KEY is not set.");

    public async Task<List<Job>> SearchAsync(string keywords, string location)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { keywords, location }), Encoding.UTF8, "application/json");
        var resp = await Http.PostAsync($"https://jooble.org/api/{_key}", content);
        resp.EnsureSuccessStatusCode();
        var parsed = JsonSerializer.Deserialize<JoobleResponse>(
            await resp.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return parsed.Jobs.Select(j => new Job(j.Title, j.Company, j.Location, j.Link, j.Snippet)).ToList();
    }

    private record JoobleResponse(int TotalCount, JoobleJob[] Jobs);
    private record JoobleJob(string Title, string Location, string Snippet, string Salary,
                             string Source, string Type, string Link, string Company, string Updated);
}