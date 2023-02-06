namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartition;

public class when_setting_recovered_to_and_it_is_at_the_head_of_the_partition : Specification
{
    FailedPartition failed_partition;
    
    Task Establish()
    {
        failed_partition = new FailedPartition(Guid.NewGuid(), 2, DateTimeOffset.UtcNow);
        failed_partition.Head = 4;
        return Task.FromResult(failed_partition);
    }

    Task Because() => Task.FromResult(failed_partition.SetRecoveredTo(4));

    [Fact] void should_be_recovered() => failed_partition.IsRecovered.ShouldBeTrue();    
}