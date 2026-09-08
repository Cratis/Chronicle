// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// A removal ends the cycle it belongs to and no other. Returning the loan at the branch that holds it opens the
/// next cycle there, while a return recorded on another branch leaves this branch's cycle exactly as it was.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_scoped_constraint_was_released(MongoDBFixture fixture) : given.a_unique_event_types_constraints_storage(fixture)
{
    static readonly EventStreamId _thisBranch = "branch-1";
    static readonly EventStreamId _anotherBranch = "branch-2";
    static readonly UniqueEventTypeConstraintDefinition _definition = DefinitionScopedTo(new ConstraintScope(EventStreamId: (EventStreamId)Marker));

    bool _isAllowedAfterAReleaseOnAnotherStream;
    EventSequenceNumber _sequenceNumberAfterAReleaseOnAnotherStream;
    bool _isAllowedAfterAReleaseOnTheSameStream;

    async Task Establish()
    {
        await Append(_checkedOutEventType, _borrower, eventStreamId: _thisBranch);
        await Append(_returnedEventType, _borrower, eventStreamId: _anotherBranch);
    }

    async Task Because()
    {
        (_isAllowedAfterAReleaseOnAnotherStream, _sequenceNumberAfterAReleaseOnAnotherStream) = await _storage.IsAllowed(_definition, _borrower, ScopeFor(_definition, eventStreamId: _thisBranch));

        await Append(_returnedEventType, _borrower, eventStreamId: _thisBranch);
        (_isAllowedAfterAReleaseOnTheSameStream, _) = await _storage.IsAllowed(_definition, _borrower, ScopeFor(_definition, eventStreamId: _thisBranch));
    }

    [Fact] void should_not_let_a_release_on_another_stream_open_this_cycle() => _isAllowedAfterAReleaseOnAnotherStream.ShouldBeFalse();
    [Fact] void should_still_report_the_covered_event_holding_the_cycle() => _sequenceNumberAfterAReleaseOnAnotherStream.ShouldEqual(new EventSequenceNumber(0));
    [Fact] void should_let_a_release_on_the_same_stream_open_the_next_cycle() => _isAllowedAfterAReleaseOnTheSameStream.ShouldBeTrue();
}
