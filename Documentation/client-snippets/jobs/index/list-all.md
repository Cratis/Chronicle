```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Jobs;

public class JobsIndexListAll(IEventStore eventStore)
{
    public async Task<IEnumerable<Job>> GetAllJobs() => await eventStore.Jobs.GetJobs();
}
```
