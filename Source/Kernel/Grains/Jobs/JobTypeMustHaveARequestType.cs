using Cratis.Chronicle.Concepts.Jobs;
namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Exception that gets thrown when a job does not have any <see cref="IJobRequest"/> type.
/// </summary>
/// <param name="jobType">The job type.</param>
/// <param name="jobClrType">The job clr <see cref="Type"/>.</param>
public class JobTypeMustHaveARequestType(JobType jobType, Type jobClrType)
    : Exception($"Job {jobType} associated with clr type {jobClrType} must have one Job Request type");