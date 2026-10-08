// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwnerKeeper.when_stopping;

public class should_stop_the_loop : given.a_keeper
{
    ISiloLifecycle _lifecycle;
    ILifecycleObserver _observer;
    Exception _exception;

    void Establish()
    {
        _owner.Ensure().Returns(Task.CompletedTask);
        _lifecycle = Substitute.For<ISiloLifecycle>();
        _lifecycle.Subscribe(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ILifecycleObserver>())
            .Returns(call =>
            {
                _observer = call.ArgAt<ILifecycleObserver>(2);
                return Substitute.For<IDisposable>();
            });
        _keeper.Participate(_lifecycle);
    }

    async Task Because()
    {
        await _observer.OnStart(CancellationToken.None);
        _exception = await Catch.Exception(() => _observer.OnStop(CancellationToken.None));
    }

    [Fact] void should_ping_on_start() => _owner.Received().Ensure();
    [Fact] void should_stop_without_error() => _exception.ShouldBeNull();
}
