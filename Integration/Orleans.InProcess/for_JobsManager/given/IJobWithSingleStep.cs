using Cratis.Chronicle.Grains.Jobs;
namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.given;

public interface IJobWithSingleStep : IJob<JobWithSingleStepRequest>
{
}
