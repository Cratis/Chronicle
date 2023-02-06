namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartition;

public class when_recovered_to_is_not_set : Specification
{
    FailedPartition failed_partition;
    
    Task Establish()
    {
        failed_partition = new FailedPartition(Guid.NewGuid(), 2, DateTimeOffset.UtcNow);
        failed_partition.Head = 4;
        return Task.FromResult(failed_partition);
    }

    [Fact] void should_not_be_recovered() => failed_partition.IsRecovered.ShouldBeFalse();  
}