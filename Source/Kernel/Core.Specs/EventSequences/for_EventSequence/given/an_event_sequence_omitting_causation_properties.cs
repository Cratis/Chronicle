// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_omitting_causation_properties : an_event_sequence
{
    protected Causation[] _requestedCausation;
    protected Causation[] _storedCausation;
    protected DateTimeOffset _occurred = new(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);

    protected override CausationPropertyRetention CausationPropertyRetention => CausationPropertyRetention.Omit;

    void Establish()
    {
        _requestedCausation =
        [
            new Causation(_occurred, "command", new Dictionary<string, string> { ["name"] = "personal value" }),
            new Causation(_occurred.AddSeconds(1), "request", new Dictionary<string, string> { ["path"] = "another value" })
        ];
        var observers = Substitute.For<IObserverDefinitionsStorage>();
        observers.GetReplayableObserversForEventTypes(Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromResult<IEnumerable<ObserverDefinition>>([]));
        _eventStoreStorage.Observers.Returns(observers);
    }

    protected Causation[] StoredFor(string methodName, int argumentIndex) =>
        _eventSequenceStorage.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == methodName)
            .Select(call => ((IEnumerable<Causation>)call.GetArguments()[argumentIndex]!).ToArray())
            .Single();

    protected Causation[] StoredAppend() => StoredFor(nameof(IEventSequenceStorage.Append), 7);
}
