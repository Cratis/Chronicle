// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.for_Reducers.given;

public class a_reducer_observing_an_event_that_can_fail(ChronicleInProcessFixture chronicleInProcessFixture, int numberOfObservations) : IntegrationSpecificationContext(chronicleInProcessFixture)
{
    public TaskCompletionSource[] Tcs;
    public ReducerThatCanFail[] Observers;
    public override IEnumerable<Type> Reducers => [typeof(ReducerThatCanFail)];
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    int _activationCount;

    protected override void ConfigureServices(IServiceCollection services)
    {
        Tcs = Enumerable.Range(0, numberOfObservations).Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        Observers = Enumerable.Range(0, numberOfObservations).Select(index => new ReducerThatCanFail(Tcs[index])).ToArray();
        services.AddTransient(_ => Observers[_activationCount++]);
    }
}
