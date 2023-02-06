namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartition;

public class when_setting_recovered_to_and_it_is_behind_the_head_of_the_partition : Specification
{
    FailedPartition failed_partition;
    
    Task Establish()
    {
        failed_partition = new FailedPartition(Guid.NewGuid(), 2, DateTimeOffset.UtcNow);
        failed_partition.Head = 4;
        return Task.FromResult(failed_partition);
    }

    Task Because() => Task.FromResult(failed_partition.SetRecoveredTo(3));

    [Fact] void should_not_be_recovered() => failed_partition.IsRecovered.ShouldBeFalse();  
}