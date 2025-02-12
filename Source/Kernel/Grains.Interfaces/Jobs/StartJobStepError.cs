namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// The type of error that occurred while performing start operation on <see cref="IJobStep"/>.
/// </summary>
public enum StartJobStepError
{
    /// <summary>
    /// Unknown error occurred.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The request type was wrong.
    /// </summary>
    NotPrepared = 1,

    /// <summary>
    /// The job step is already running.
    /// </summary>
    AlreadyRunning = 2
}