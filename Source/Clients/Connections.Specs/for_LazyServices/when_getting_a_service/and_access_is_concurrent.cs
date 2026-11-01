// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Chronicle.Connections.for_LazyServices.when_getting_a_service;

public class and_access_is_concurrent : given.a_lazy_services
{
    readonly IEventSequences[] _results = new IEventSequences[16];
    readonly ConcurrentQueue<bool> _factoryWaits = new();
    readonly ConcurrentQueue<Exception> _errors = new();

    void Because()
    {
        using var barrier = new Barrier(_results.Length);
        _factory.OnCreated = () => _factoryWaits.Enqueue(barrier.SignalAndWait(TimeSpan.FromSeconds(5)));
        var threads = Enumerable.Range(0, _results.Length).Select(index => new Thread(() =>
        {
            var error = Catch.Exception(() => _results[index] = _services.Sequences);
            if (error is not null)
            {
                _errors.Enqueue(error);
            }
        }) { IsBackground = true }).ToArray();

        foreach (var thread in threads)
        {
            thread.Start();
        }

        var allThreadsJoined = true;
        foreach (var thread in threads)
        {
            allThreadsJoined &= thread.Join(TimeSpan.FromSeconds(5));
        }

        allThreadsJoined.ShouldBeTrue();
        _errors.ShouldBeEmpty();
        _factoryWaits.Count.ShouldEqual(_results.Length);
        _factoryWaits.All(_ => _).ShouldBeTrue();
    }

    [Fact] void should_create_a_proxy_for_every_caller() => _factory.Creations.Count.ShouldEqual(_results.Length);
    [Fact] void should_hand_every_caller_the_same_instance() => _results.All(_ => ReferenceEquals(_, _services.Sequences)).ShouldBeTrue();
    [Fact] void should_publish_one_of_the_created_proxies() => _factory.Creations.Any(_ => ReferenceEquals(_.Service, _services.Sequences)).ShouldBeTrue();
}
