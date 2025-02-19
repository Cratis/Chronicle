namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.given;

public record TheJobStepRequest(bool ShouldFail, TimeSpan WaitTime, int WaitCount);