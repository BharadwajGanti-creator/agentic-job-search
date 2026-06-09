using System.Text.Json;

public static class Eval
{
    public static async Task RunAsync()
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var profile = JsonSerializer.Deserialize<ResumeProfile>(File.ReadAllText("resume_profile.json"), opts)!;
        var golden  = JsonSerializer.Deserialize<GoldenItem[]>(File.ReadAllText("golden_set.json"), opts)!;

        string target = "Backend / Software Engineer (SDE) roles in Dublin, Ireland. " +
                        "Open to .NET, C#, and backend positions at mid level.";

        var jobs = golden.Select(g => new Job(g.Title, g.Company, g.Location, "", "")).ToList();
        var scores = await new JobMatcher().ScoreJobsAsync(profile, target, jobs);
        var scoreByIndex = scores.Where(s => s.Index >= 0 && s.Index < jobs.Count)
                                 .ToDictionary(s => s.Index, s => s.FitScore);

        const int threshold = 60;   // brain calls a job "relevant" at >= 60
        int tp = 0, tn = 0, fp = 0, fn = 0;

        Console.WriteLine($"{"Job",-46}{"You",-6}{"Brain",-7}{"Score",-7}Result");
        Console.WriteLine(new string('-', 86));
        for (int i = 0; i < golden.Length; i++)
        {
            int score = scoreByIndex.TryGetValue(i, out var sc) ? sc : 0;
            bool brain = score >= threshold;
            bool you = golden[i].Relevant;

            string result =
                (you && brain) ? "agree (hit)" :
                (!you && !brain) ? "agree (skip)" :
                (!you && brain) ? "DISAGREE (false positive)" : "DISAGREE (missed)";
            if (you && brain) tp++; else if (!you && !brain) tn++; else if (!you && brain) fp++; else fn++;

            string name = $"{golden[i].Title} @ {golden[i].Company}";
            if (name.Length > 45) name = name[..45];
            Console.WriteLine($"{name,-46}{(you ? "yes" : "no"),-6}{(brain ? "yes" : "no"),-7}{score,-7}{result}");
        }

        int total = golden.Length;
        Console.WriteLine(new string('-', 86));
        Console.WriteLine($"Accuracy:  {(double)(tp + tn) / total:P0}  ({tp + tn}/{total} agree with your labels)");
        Console.WriteLine($"Precision: {((tp + fp) == 0 ? 0 : (double)tp / (tp + fp)):P0}  (when brain says relevant, how often it's right)");
        Console.WriteLine($"Recall:    {((tp + fn) == 0 ? 0 : (double)tp / (tp + fn)):P0}  (of jobs you marked relevant, how many it caught)");
    }
}