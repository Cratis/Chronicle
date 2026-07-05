```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Jobs;

public class JobsIndexStopResumeDelete(IEventStore eventStore)
{
    public async Task StopJob(JobId jobId) => await eventStore.Jobs.Stop(jobId);

    public async Task ResumeJob(JobId jobId) => await eventStore.Jobs.Resume(jobId);

    public async Task DeleteJob(JobId jobId) => await eventStore.Jobs.Delete(jobId);
}
```
