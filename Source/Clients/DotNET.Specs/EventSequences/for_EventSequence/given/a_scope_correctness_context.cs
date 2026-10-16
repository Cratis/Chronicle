// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_scope_correctness_context : a_definition_backed_batch
{
    protected ConcurrencyOptions _options;
    protected PreparedEvent _prepared;
    protected Contracts.Sequences.ConcurrencyScope _wireScope;
    protected int _appendCalls;

    protected static Type Definition => typeof(EventSources.for_EventSources.ShoppingCartEventSource);

    async Task Establish()
    {
        _options = new();
        _concurrencyScopeStrategies.Options.Returns(_options);
        _eventSequence = new(
            "scopes",
            "specs",
            EventSequenceId.Log,
            _connection,
            _eventTypes,
            _constraints,
            _eventSerializer,
            _correlationIdAccessor,
            _concurrencyScopeStrategies,
            _causationManager,
            _unitOfWorkManager,
            _identityProvider,
            JsonSerializerOptions.Default,
            eventSources: _eventSources);
        _prepared = await _eventSequence.Prepare("item");
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            Capture(call.Arg<Contracts.Sequences.AppendRequest>().ConcurrencyScope);
            return CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 42, ConstraintViolations = [], Errors = [] });
        });
        _sequences.AppendWithNamedTags(Arg.Any<Contracts.Sequences.AppendWithNamedTagsRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            Capture(call.Arg<Contracts.Sequences.AppendWithNamedTagsRequest>().ConcurrencyScope);
            return SingleResponse();
        });
        _sequences.AppendMany(Arg.Any<Contracts.Sequences.AppendManyRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            Capture(call.Arg<Contracts.Sequences.AppendManyRequest>().ConcurrencyScope);
            return BatchResponse();
        });
        _sequences.AppendManyWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyWithNamedTagsRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            Capture(call.Arg<Contracts.Sequences.AppendManyWithNamedTagsRequest>().ConcurrencyScope);
            return BatchResponse();
        });
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            Capture(call.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>().ConcurrencyScopes.Single().Scope);
            return BatchResponse();
        });
        _sequences.AppendManyForEventSourcesWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            Capture(call.Arg<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>().ConcurrencyScopes.Single().Scope);
            return BatchResponse();
        });
    }

    protected Task AppendOnPath(int path, ConcurrencyScope scope) => path switch
    {
        0 => _eventSequence.AppendPrepared(_sourceId, _prepared, concurrencyScope: scope),
        1 => _eventSequence.AppendPreparedThroughEventSource(Definition, _sourceId, _prepared, "Payment", concurrencyScope: scope),
        2 => _eventSequence.Append(_sourceId, "item", concurrencyScope: scope),
        3 => _eventSequence.AppendThroughEventSource(Definition, _sourceId, "item", "Payment", concurrencyScope: scope),
        4 => _eventSequence.AppendManyThroughEventSource(Definition, _sourceId, ["item"], "Payment", concurrencyScope: scope),
        5 => _eventSequence.AppendWithNamedTags(_sourceId, "item", [new("name", "value")], concurrencyScope: scope),
        6 => _eventSequence.AppendMany(_sourceId, ["item"], concurrencyScope: scope),
        7 => _eventSequence.AppendManyWithNamedTags(_sourceId, ["item"], [new("name", "value")], concurrencyScope: scope),
        8 => _eventSequence.AppendMany([Payment()], concurrencyScopes: new Dictionary<EventSourceId, ConcurrencyScope> { [_sourceId] = scope }),
        9 => _eventSequence.AppendManyWithNamedTags([Payment()], [new("name", "value")], concurrencyScopes: new Dictionary<EventSourceId, ConcurrencyScope> { [_sourceId] = scope }),
        _ => ((IEventSequenceWithReadProtection)_eventSequence).AppendMany(
            [Payment()],
            [],
            new Dictionary<EventSourceId, ConcurrencyScope> { [_sourceId] = scope },
            new(new("scopes", "specs", EventSequenceId.Log), [new(_sourceId, ReadProtectionContributionKind.Explicit, scope, new("scopes", "specs", EventSequenceId.Log), "explicit")], _ => { }))
    };

    void Capture(Contracts.Sequences.ConcurrencyScope scope)
    {
        _wireScope = scope;
        _appendCalls++;
    }

    static CommandResult<Contracts.Sequences.AppendResponse> SingleResponse() =>
        CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 42, ConstraintViolations = [], Errors = [] });

    static CommandResult<Contracts.Sequences.AppendManyResponse> BatchResponse() =>
        CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), new() { SequenceNumbers = [42], ConstraintViolations = [], ConcurrencyViolations = [], Errors = [] });
}
