using System.Collections.Immutable;
namespace Cratis.Chronicle.Grains.Jobs.for_Job.given;

public class SomeJob : Job<SomeRequest, SomeJobState>
{
    public List<JobStepDetails> StepsToPrepare = [];
    public bool OnCompletedThrows;
    public bool ShouldBeRemovedAfterCompleted;
    public bool ShouldBeResumable;
    

    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(SomeRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(StepsToPrepare.ToImmutableList());

    protected override bool RemoveAfterCompleted => ShouldBeRemovedAfterCompleted;
    public override Task OnCompleted() => OnCompletedThrows
        ? Task.FromException(new Exception())
        : Task.CompletedTask;

    protected override Task<bool> CanResume() => Task.FromResult(ShouldBeResumable);
}