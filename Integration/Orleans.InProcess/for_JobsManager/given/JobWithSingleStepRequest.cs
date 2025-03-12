using Cratis.Chronicle.Concepts.Jobs;
namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.given;

public record JobWithSingleStepRequest(bool KeepAfterCompleted = false, bool ShouldFail = false, TimeSpan? WaitTime = null, int WaitCount = 0) : IJobRequest;