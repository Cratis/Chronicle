// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching;

public class and_the_delegate_is_cancelled : given.a_reactor_dispatcher
{
    int _calls;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await _dispatcher.Dispatch(
            _handler,
            (_, _) =>
            {
                _calls++;
                return Task.CompletedTask;
            },
            EventsWith(1, 2),
            "ns",
            Publish,
            cancellation.Token);
    }

    [Fact] void should_not_call_the_delegate() => _calls.ShouldEqual(0);
    [Fact] void should_not_publish_a_result() => _published.ShouldBeEmpty();
}
