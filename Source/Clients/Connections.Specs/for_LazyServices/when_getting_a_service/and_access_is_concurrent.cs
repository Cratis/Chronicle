// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Chronicle.Connections.for_LazyServices.when_getting_a_service;

public class and_access_is_concurrent : given.a_lazy_services
{
    readonly IEventSequences[] _results = new IEventSequences[16];

    void Because()
    {
        using var barrier = new Barrier(_results.Length);
        var threads = Enumerable.Range(0, _results.Length).Select(index => new Thread(() =>
        {
            barrier.SignalAndWait();
            _results[index] = _services.Sequences;
        })).ToArray();

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }
    }

    [Fact] void should_hand_every_caller_the_same_instance() => _results.All(_ => ReferenceEquals(_, _services.Sequences)).ShouldBeTrue();
}
