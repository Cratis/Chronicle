// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_identifying_read_model_key_for_root_join : Specification
{
    AppendedEvent _joinedEvent;
    Key _result;
    IProjection _projection;
    IEventSequenceStorage _storage;
    ISink _sink;
    KeyResolvers _keyResolvers;

    const string GroupId = "group-123";
    const string UserId = "user-456";
    PropertyPath _queriedPropertyPath;

    void Establish()
    {
        _keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);

        _joinedEvent = new(
            new(
                new EventType("02405794-91e7-4e4f-8ad1-f043070ca297", 1),
                EventSourceType.Default,
                GroupId,
                EventStreamType.All,
                EventStreamId.Default,
                1,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            new
            {
                name = "Updated Name"
            }.AsExpandoObject());

        _projection = Substitute.For<IProjection>();
        _projection.HasParent.Returns(false);

        _storage = Substitute.For<IEventSequenceStorage>();
        _sink = Substitute.For<ISink>();
        _sink.When(x => x.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), GroupId))
            .Do(callInfo => _queriedPropertyPath = callInfo.ArgAt<PropertyPath>(0));
        _sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), GroupId)
            .Returns(new Option<Key>(new Key(UserId, ArrayIndexers.NoIndexers)));
    }

    async Task Because()
    {
        var keyResult = await _keyResolvers.ForJoin(
            _projection,
            _keyResolvers.FromEventSourceId,
            "groupId",
            "id")(_storage, _sink, _joinedEvent);
        _result = (keyResult as ResolvedKey)!.Key;
    }

    [Fact]
    void should_query_using_join_on_property() =>
        _queriedPropertyPath.ShouldEqual((PropertyPath)"groupId");

    [Fact]
    void should_resolve_to_the_root_read_model_key() =>
        _result.ShouldEqual(new Key(UserId, ArrayIndexers.NoIndexers));
}