// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// Combining dimensions narrows the cycle to the whole tuple: every declared dimension has to match for an
/// already-appended covered event to belong to the same cycle, so one differing dimension opens a separate one.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_constraint_is_scoped_to_every_dimension(MongoDBFixture fixture) : given.a_unique_event_types_constraints_storage(fixture)
{
    static readonly EventSourceType _loan = "loan";
    static readonly EventStreamType _loans = "loans";
    static readonly EventStreamId _thisBranch = "branch-1";
    static readonly EventStreamId _anotherBranch = "branch-2";
    static readonly UniqueEventTypeConstraintDefinition _definition = DefinitionScopedTo(EveryDimension);

    bool _isAllowedForTheSameTuple;
    EventSequenceNumber _sequenceNumberForTheSameTuple;
    bool _isAllowedWhenOneDimensionDiffers;

    async Task Establish() => await Append(_checkedOutEventType, _borrower, _loan, _loans, _thisBranch);

    async Task Because()
    {
        (_isAllowedForTheSameTuple, _sequenceNumberForTheSameTuple) = await _storage.IsAllowedWithinScope(_definition, _borrower, ScopeFor(_definition, _loan, _loans, _thisBranch));
        (_isAllowedWhenOneDimensionDiffers, _) = await _storage.IsAllowedWithinScope(_definition, _borrower, ScopeFor(_definition, _loan, _loans, _anotherBranch));
    }

    [Fact] void should_not_allow_a_covered_event_with_the_same_dimensions() => _isAllowedForTheSameTuple.ShouldBeFalse();
    [Fact] void should_report_the_covered_event_holding_the_cycle() => _sequenceNumberForTheSameTuple.ShouldEqual(new EventSequenceNumber(0));
    [Fact] void should_allow_a_covered_event_when_one_dimension_differs() => _isAllowedWhenOneDimensionDiffers.ShouldBeTrue();
}
