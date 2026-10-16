// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_a_typed_custom_concurrency_strategy : given.an_event_sequence
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_pass_the_original_typed_id_or_use_its_explicit_scope(bool explicitScope)
    {
        var source = new BookId("book");
        var strategy = new TypedStrategy();
        _concurrencyScopeStrategies.GetFor(Arg.Any<IEventSequence>()).Returns(strategy);
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("event", EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(Identity.NotSet);
        Contracts.Sequences.AppendManyForEventSourcesRequest command = null!;
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default)
            .Returns(call =>
            {
                command = call.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>();
                return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), new() { SequenceNumbers = [1, 2] });
            });
        Dictionary<EventSourceId, ConcurrencyScope>? scopes = explicitScope ? new() { [source] = new(7, source) } : null;

        await _eventSequence.AppendMany([new(source, "first"), new(new BookId("book"), "second")], concurrencyScopes: scopes);

        command.ConcurrencyScopes.Count().ShouldEqual(1);
        command.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(explicitScope ? 7UL : 42UL);
        if (explicitScope) strategy.Source.ShouldBeNull();
        else Assert.Same(source, strategy.Source);
    }

    sealed class TypedStrategy : IConcurrencyScopeStrategy
    {
        public BookId? Source { get; private set; }

        public Task<ConcurrencyScope> GetScope(EventSourceId eventSourceId, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, IEnumerable<EventType>? eventTypes = default)
        {
            Source = (BookId)eventSourceId;
            return Task.FromResult(new ConcurrencyScope(42, Source));
        }
    }

    record BookId(string Value) : EventSourceId(Value);
}
