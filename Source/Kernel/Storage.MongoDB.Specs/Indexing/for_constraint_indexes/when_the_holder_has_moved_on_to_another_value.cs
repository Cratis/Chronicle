// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_constraint_indexes;

/// <summary>
/// An event source holds at most one value per constraint and scope, so saving a new value releases the one it held.
/// This pins the behavior every <see cref="Cratis.Chronicle.Storage.Events.Constraints.IUniqueConstraintsStorage"/>
/// implementation has to agree on.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_the_holder_has_moved_on_to_another_value(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    const string ConstraintName = "released-constraint";
    static readonly EventSourceId _holder = "es-1";
    static readonly EventSourceId _otherEventSourceId = "es-2";
    static readonly UniqueConstraintValue _releasedValue = "released-value";
    static readonly UniqueConstraintValue _currentValue = "current-value";
    static readonly EventSequenceNumber _claimedAt = 42UL;

    bool _isAllowedToClaimReleasedValue;
    bool _isAllowedToClaimCurrentValue;
    bool _isAllowedForHolder;
    EventSequenceNumber _sequenceNumberForHolder;

    async Task Because()
    {
        var definition = new UniqueConstraintDefinition(ConstraintName, []);
        var storage = new UniqueConstraintsStorage(_database, EventSequenceId.Log, Substitute.For<ILogger<UniqueConstraintsStorage>>());
        await storage.Save(_holder, ConstraintName, EventSequenceNumber.First, _releasedValue);
        await storage.Save(_holder, ConstraintName, _claimedAt, _currentValue);

        (_isAllowedToClaimReleasedValue, _) = await storage.IsAllowed(_otherEventSourceId, definition, _releasedValue);
        (_isAllowedToClaimCurrentValue, _) = await storage.IsAllowed(_otherEventSourceId, definition, _currentValue);
        (_isAllowedForHolder, _sequenceNumberForHolder) = await storage.IsAllowed(_holder, definition, _currentValue);
    }

    [Fact] void should_allow_another_event_source_to_claim_the_released_value() => _isAllowedToClaimReleasedValue.ShouldBeTrue();
    [Fact] void should_not_allow_another_event_source_to_claim_the_current_value() => _isAllowedToClaimCurrentValue.ShouldBeFalse();
    [Fact] void should_allow_the_holder_to_reclaim_its_current_value() => _isAllowedForHolder.ShouldBeTrue();
    [Fact] void should_report_the_sequence_number_the_holder_claimed_at() => _sequenceNumberForHolder.ShouldEqual(_claimedAt);
}
