```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Jobs;

public class JobsIndexGetSteps(IEventStore eventStore)
{
    public async Task<IEnumerable<JobStep>?> GetSteps(JobId jobId)
    {
        var job = await eventStore.Jobs.GetJob(jobId);
        return job is null ? null : await job.GetJobSteps();
    }
}
```
