// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using StoredEffectReceipt = Cratis.Chronicle.Storage.Occurrences.EffectReceipt;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_namespace_bound_receipt_sequence : a_namespace_bound_sequence
{
    protected readonly IdempotencyKey _key = new("namespace-key");
    protected readonly Occurrences.EffectReceipt _receipt = new("occurrence", "command", 1, "{}");
    protected readonly List<NamespaceCommitContext> _receiptContexts = [];
    protected Storage.InMemory.EventSequences.EventSequenceStorage _durable = null!;

    protected ConcurrencyScopeConjunctions Conjunctions => new([]);

    void Establish()
    {
        _durable = new(EventStore, EventStoreNamespace, SequenceId, _identityStorage);
        _eventSequenceStorage.GetEventAt(Arg.Any<EventSequenceNumber>()).Returns(call => _durable.GetEventAt(call.Arg<EventSequenceNumber>()));
        ConfigurePublicationStorage();
        ConfigureEffectStorage();
        _complianceManager.LocateProtectedValues(Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(_ => []);
        _complianceManager.ApplyReportingKeys(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => (call.Arg<JsonObject>().DeepClone().AsObject(), (IReadOnlyList<ProtectedValueUse>)[]));
    }

    void ConfigurePublicationStorage()
    {
        _fenced.TryGetPublication(Arg.Any<NamespaceCommitContext>(), Arg.Any<EventPublication>())
            .Returns(call => Capture(call.Arg<NamespaceCommitContext>(), () => _durable.TryGetPublication(call.Arg<EventPublication>())));
        _fenced.TryGetPublications(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<string>>())
            .Returns(call => Capture(call.Arg<NamespaceCommitContext>(), () => _durable.TryGetPublications(call.Arg<IEnumerable<string>>())));
        _fenced.AppendPublication(Arg.Any<NamespaceCommitContext>(), Arg.Any<EventPublication>(), Arg.Any<EventToAppendToStorage>())
            .Returns(call => Capture(call.Arg<NamespaceCommitContext>(), () => _durable.AppendPublication(call.Arg<EventPublication>(), call.Arg<EventToAppendToStorage>())));
        _fenced.AppendManyWithIdentities(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(call => Capture(call.Arg<NamespaceCommitContext>(), () => _durable.AppendManyWithIdentities(call.Arg<IEnumerable<EventToAppendToStorage>>())));
    }

    void ConfigureEffectStorage()
    {
        _fenced.GetEffectReceipt(Arg.Any<NamespaceCommitContext>(), Arg.Any<string>())
            .Returns(call => Capture(call.Arg<NamespaceCommitContext>(), () => _durable.GetEffectReceipt(call.Arg<string>())));
        _fenced.AppendManyWithEffectReceipt(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<EventToAppendToStorage>>(), Arg.Any<StoredEffectReceipt>())
            .Returns(call => Capture(call.Arg<NamespaceCommitContext>(), () => _durable.AppendManyWithEffectReceipt(call.Arg<IEnumerable<EventToAppendToStorage>>(), call.Arg<StoredEffectReceipt>())));
    }

    Task<T> Capture<T>(NamespaceCommitContext context, Func<Task<T>> operation)
    {
        _receiptContexts.Add(context);
        return operation();
    }
}
