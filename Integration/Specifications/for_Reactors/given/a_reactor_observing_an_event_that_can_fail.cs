// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Integration.Specifications.for_Reactors.given;

public class a_reactor_observing_an_event_that_can_fail(ChronicleFixture chronicleFixture, int numberOfObservations) : Specification<ChronicleFixture>(chronicleFixture)
{
    public TaskCompletionSource[] Tcs;
    public ReactorThatCanFail[] Observers;
    public override IEnumerable<Type> Reactors => [typeof(ReactorThatCanFail)];
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    int _activationCount;

    protected override void ConfigureServices(IServiceCollection services)
    {
        Tcs = Enumerable.Range(0, numberOfObservations).Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        Observers = Enumerable.Range(0, numberOfObservations).Select(index => new ReactorThatCanFail(Tcs[index])).ToArray();
        services.AddTransient(_ => Observers[_activationCount++]);
    }

    protected Task<IEnumerable<FailedPartition>> GetFailedPartitions() => EventStore.Reactors.GetFailedPartitionsFor<ReactorThatCanFail>();
}
