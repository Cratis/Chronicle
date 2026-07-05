```csharp
public static class ContributingKernelOneOfUsage
{
    public static async Task Run(IContributingKernelJobs jobsService, ContributingKernelGetJobRequest request)
    {
        var result = await jobsService.GetJob(request);

        if (result.Value0 is not null)
        {
            // Handle Job
            var job = result.Value0;
            Console.WriteLine($"Job: {job.Id}");
        }
        else if (result.Value1 is not null)
        {
            // Handle JobError
            var error = result.Value1;
            Console.WriteLine($"Error: {error.Message}");
        }

        // Or use the Value property to get whichever is set
        object value = result.Value;
        Console.WriteLine(value);
    }
}
```
