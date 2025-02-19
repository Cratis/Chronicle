using Cratis.Chronicle.Storage.Jobs;
namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.given;

public class TheJobStepState : JobStepState
{
    public bool ShouldFail { get; set; }
    public TimeSpan WaitTime { get; set; } = TimeSpan.Zero;
    public int WaitCount { get; set; }
    public bool Completed { get; set; }
    public bool Failed { get; set; }
    public int NumTimesStopped { get; set; }
    public int NumTimesPerformCalled { get; set; }
}