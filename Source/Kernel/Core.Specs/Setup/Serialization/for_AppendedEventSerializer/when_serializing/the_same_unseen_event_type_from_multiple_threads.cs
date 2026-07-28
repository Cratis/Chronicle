// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer.when_serializing;

public class the_same_unseen_event_type_from_multiple_threads : given.a_serializer_for_appended_events
{
    const int NumberOfThreads = 8;
    static readonly TimeSpan _maxTimeHeldInLookup = TimeSpan.FromMilliseconds(300);

    CountdownEvent _arrivals;
    ConcurrentBag<Exception> _failures;

    void Establish()
    {
        _arrivals = new CountdownEvent(NumberOfThreads);
        _failures = [];

        // Every thread that reaches the storage lookup parks there until all of them have arrived, so a
        // serializer that resolves per call is held long enough for all of them to overlap.
        _eventTypesStorage
            .GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>())
            .Returns(_ =>
            {
                _arrivals.Signal();
                _arrivals.Wait(_maxTimeHeldInLookup);
                return Task.FromResult(_schema);
            });
    }

    void Because()
    {
        var threads = Enumerable.Range(0, NumberOfThreads).Select(_ => new Thread(() =>
        {
            try
            {
                Serialize(AnEvent());
            }
            catch (Exception exception)
            {
                _failures.Add(exception);
            }
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

    void Destroy() => _arrivals.Dispose();

    [Fact] void should_look_up_the_schema_only_once() => SchemaLookups().ShouldEqual(1);
    [Fact] void should_serialize_every_event() => _failures.ShouldBeEmpty();
}
