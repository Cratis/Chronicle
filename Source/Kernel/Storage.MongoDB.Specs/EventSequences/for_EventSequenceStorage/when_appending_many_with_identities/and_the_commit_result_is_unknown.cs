// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_many_with_identities;

public class and_the_commit_result_is_unknown : given.an_event_sequence_storage
{
    MongoException _commitError;
    Exception _error;
    IClientSessionHandle _session;
    EventToAppendToStorage _event;

    void Establish()
    {
        _commitError = new MongoException("Commit acknowledgment lost");
        _commitError.AddErrorLabel("UnknownTransactionCommitResult");
        _commitError.AddErrorLabel("TransientTransactionError");
        _session = Substitute.For<IClientSessionHandle>();
        var client = Substitute.For<IMongoClient>();
        _database.Client.Returns(client);
        client.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>()).Returns(_session);
        _session.CommitTransactionAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(_commitError));
        var type = new EventType("event", EventTypeGeneration.First);
        _eventTypesStorage.GetFor(type.Id, Arg.Any<EventTypeGeneration?>()).Returns(new EventTypeSchema(type, EventTypeOwner.None, EventTypeSource.Unknown, new JsonSchema()));
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject());
        _identityStorage.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        _event = new(0, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, type, CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, new ExpandoObject(), EventHash.NotSet)
        {
            Identity = new EventPublication("identity", "fingerprint")
        };
    }

    async Task Because() => _error = await Catch.Exception(() => _storage.AppendManyWithIdentities([_event]));

    [Fact] void should_preserve_the_unknown_outcome_even_with_a_transient_label() => _error.ShouldBeSame(_commitError);
    [Fact] void should_not_claim_the_transaction_was_aborted() => _session.DidNotReceive().AbortTransactionAsync(Arg.Any<CancellationToken>());
}
