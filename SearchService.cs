public static class SearchService
{
    public static async Task<SearchTrace> RunAsync(
        ResumeProfile profile, string target, JobMatcher matcher, IJobSource source, string location)
    {
        string[] queries = await matcher.PlanQueriesAsync(profile, target);

        var seen = new HashSet<string>();
        var jobs = new List<Job>();
        int fetched = 0;
        foreach (var q in queries)
            foreach (var job in await source.SearchAsync(q, location))
            {
                fetched++;
                if (seen.Add(job.Link)) jobs.Add(job);
            }

        var scores = await matcher.ScoreJobsAsync(profile, target, jobs);

        var ranked = scores
            .Where(s => s.Index >= 0 && s.Index < jobs.Count)
            .OrderByDescending(s => s.FitScore)
            .Select(s =>
            {
                var j = jobs[s.Index];
                return new RankedJob(j.Title, j.Company, j.Location, j.Link, s.FitScore, s.Reason, s.Missing);
            })
            .ToList();

        return new SearchTrace(queries, fetched, jobs.Count, ranked);
    }
}