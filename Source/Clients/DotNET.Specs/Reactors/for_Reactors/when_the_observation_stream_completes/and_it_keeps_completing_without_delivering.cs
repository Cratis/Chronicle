// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_the_observation_stream_completes;

public class and_it_keeps_completing_without_delivering : given.a_reactor_whose_stream_completes
{
    readonly List<TimeSpan> _waits = [];

    protected override TimeSpan ReconnectDelay => TimeSpan.FromSeconds(1);

    protected override Func<double> ReconnectJitter => () => 1;

    protected override Func<TimeSpan, CancellationToken, Task> WaitBeforeReconnect => (wait, _) =>
    {
        _waits.Add(wait);
        return Task.CompletedTask;
    };

    async Task Because()
    {
        await _reactors.Register();
        EndTheFirstStream();
        for (var round = 0; round < 3 && round < _reconnectWork.Count; round++)
        {
            await _reconnectWork[round]();
        }
    }

    [Fact] void should_double_the_wait_for_every_stream_that_ended_without_working() =>
        _waits.ShouldContainOnly(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4));

    protected override IObservable<EventsToObserve> ReRegister()
    {
        _registrationsAfterCompletion++;
        return Observable.Empty<EventsToObserve>();
    }
}
