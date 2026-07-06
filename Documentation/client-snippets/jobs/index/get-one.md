```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Jobs;

public class JobsIndexGetOne(IEventStore eventStore)
{
    public async Task<Job?> GetJob(JobId jobId) => await eventStore.Jobs.GetJob(jobId);
}
```
