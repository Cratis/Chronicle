// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using MongoDB.Bson;
using JsonExpandoObjectConverter = Cratis.Chronicle.Json.IExpandoObjectConverter;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.given;

public class an_event_converter : Specification
{
    protected IEventTypesStorage _eventTypesStorage;
    protected IIdentityStorage _identityStorage;
    protected JsonExpandoObjectConverter _expandoObjectConverter;
    protected EventConverter _converter;

    void Establish()
    {
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _identityStorage = Substitute.For<IIdentityStorage>();
        _expandoObjectConverter = Substitute.For<JsonExpandoObjectConverter>();

        _identityStorage.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.NotSet);

        _converter = new EventConverter(
            EventStoreName.NotSet,
            EventStoreNamespaceName.NotSet,
            _eventTypesStorage,
            _identityStorage,
            _expandoObjectConverter);
    }

    protected static Event CreateEvent(Subject? subject = null) =>
        new(
            0UL,
            CorrelationId.NotSet,
            [],
            [IdentityId.NotSet],
            new EventTypeId(Guid.NewGuid().ToString()),
            DateTimeOffset.UtcNow,
            EventSourceType.Default,
            "test-source",
            EventStreamType.All,
            EventStreamId.Default,
            [],
            new Dictionary<string, BsonDocument>
            {
                ["1"] = BsonDocument.Parse("{\"name\":\"test\"}")
            },
            new Dictionary<string, string> { ["1"] = "hash" },
            [],
            subject);
}
