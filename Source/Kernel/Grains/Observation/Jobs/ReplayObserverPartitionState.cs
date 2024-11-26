using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Jobs;
namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents the state for a <see cref="ReplayObserverPartition"/> job.
/// </summary>
public class ReplayObserverPartitionState : JobState
{
    /// <summary>
    /// Gets or sets the event sequence number of the last handled event.
    /// </summary>
    public EventSequenceNumber LastHandledEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;
}