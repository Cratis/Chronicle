// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_stopping;

public class and_registrations_are_pending : given.a_startup_task
{
    ILifecycleObserver _observer;

    void Establish()
    {
        var lifecycle = Substitute.For<ISiloLifecycle>();
        lifecycle.When(_ => _.Subscribe(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ILifecycleObserver>()))
            .Do(call => _observer = call.Arg<ILifecycleObserver>());
        _startupTask.Participate(lifecycle);
    }

    Task Because() => _observer.OnStop(CancellationToken.None);

    [Fact] void should_stop_background_retries() => _kernelProjectionRetries.Received(1).Stop();
}
