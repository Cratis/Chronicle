// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed_with_a_flattened_scope_key;

/// <summary>
/// Calling this storage's original member with a real key is the one case that behaves differently than it used to,
/// and deliberately so: that call could only ever be answered against the wrong cycle, because the key cannot be
/// turned back into dimensions without reintroducing the aliasing the typed scope exists to remove. Failing closed
/// says so instead of quietly answering, and says it without putting the key - which is built from event source
/// type, stream type and stream id, all caller data - into the message.
/// </summary>
public class and_the_key_names_a_scope : given.a_unique_event_types_constraints_storage
{
    static readonly EventSourceType _eventSourceType = "loan-holder-nin-01019012345";
    static readonly EventStreamType _eventStreamType = "loans";
    static readonly EventStreamId _eventStreamId = "branch-oslo";
    static readonly UniqueEventTypeConstraintDefinition _definition = DefinitionScopedTo(EveryDimension);

    string _scopeKey;
    Exception _exception;

    async Task Establish()
    {
        _scopeKey = _definition.Scope.BuildScopeKey(_eventSourceType, _eventStreamType, _eventStreamId);
        await Append(0, _checkedOutEventType, _borrower, _eventSourceType, _eventStreamType, _eventStreamId);
    }

    async Task Because() => _exception = await Catch.Exception(async () => await _storage.IsAllowed(_definition, _borrower, _scopeKey));

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_point_at_the_typed_member() => _exception.Message.ShouldContain(nameof(_storage.IsAllowedWithinScope));
    [Fact] void should_not_leak_the_key() => _exception.Message.ShouldNotContain(_scopeKey);
    [Fact] void should_not_leak_the_event_source_type() => _exception.Message.ShouldNotContain(_eventSourceType.Value);
    [Fact] void should_not_leak_the_event_stream_type() => _exception.Message.ShouldNotContain(_eventStreamType.Value);
    [Fact] void should_not_leak_the_event_stream_id() => _exception.Message.ShouldNotContain(_eventStreamId.Value);
    [Fact] void should_not_leak_the_event_source_id() => _exception.Message.ShouldNotContain(_borrower.Value);
}
