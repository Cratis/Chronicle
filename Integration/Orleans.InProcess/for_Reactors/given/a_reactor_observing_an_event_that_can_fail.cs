// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.given;

public class a_reactor_observing_an_event_that_can_fail(GlobalFixture globalFixture, int numberOfObservations) : IntegrationSpecificationContext(globalFixture)
{
    public TaskCompletionSource[] Tcs;
    public ReactorThatCanFail[] Observers;
    public override IEnumerable<Type> Reactors => [typeof(ReactorThatCanFail)];
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    public ReactorId ObserverId;
    int _activationCount;

    protected override void ConfigureServices(IServiceCollection services)
    {
        Tcs = Enumerable.Range(0, numberOfObservations).Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        Observers = Enumerable.Range(0, numberOfObservations).Select(index => new ReactorThatCanFail(Tcs[index])).ToArray();
        services.AddTransient(_ => Observers[_activationCount++]);
    }

    async Task Establish()
    {
        var state = await EventStore.Reactors.GetState<ReactorThatCanFail>();
        ObserverId = state.Id;
    }

    protected Task<IEnumerable<FailedPartition>> GetFailedPartitions() => EventStore.FailedPartitions.GetFailedPartitionsFor(ObserverId);
}
