// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBatchClaims.when_claiming;

/// <summary>
/// Characterization of current behavior, which is believed to be wrong - see the assertion remarks.
/// The accumulator keys claims by value alone and never releases one, so an event source that moves to a new value
/// earlier in the same batch keeps holding the value it left behind.
/// </summary>
public class and_the_claimant_moved_to_another_value_earlier_in_the_batch : Specification
{
    static readonly ConstraintName _constraintName = "some-constraint";
    static readonly UniqueConstraintValue _releasedValue = "released-value";
    static readonly UniqueConstraintValue _currentValue = "current-value";
    static readonly EventSourceId _holder = "first-source";
    static readonly EventSourceId _otherEventSourceId = "second-source";

    ConstraintBatchClaims _claims;
    bool _holderClaimedTheReleasedValue;
    bool _holderClaimedTheCurrentValue;
    bool _otherClaimedTheReleasedValue;

    void Establish() => _claims = new();

    void Because()
    {
        _holderClaimedTheReleasedValue = _claims.TryClaim(_constraintName, string.Empty, _releasedValue, _holder);
        _holderClaimedTheCurrentValue = _claims.TryClaim(_constraintName, string.Empty, _currentValue, _holder);
        _otherClaimedTheReleasedValue = _claims.TryClaim(_constraintName, string.Empty, _releasedValue, _otherEventSourceId);
    }

    [Fact] void should_let_the_holder_claim_the_first_value() => _holderClaimedTheReleasedValue.ShouldBeTrue();
    [Fact] void should_let_the_holder_move_to_the_second_value() => _holderClaimedTheCurrentValue.ShouldBeTrue();

    /// <summary>
    /// Pins the defect. The batch's constraint index updates are applied in event order by
    /// <c>EventSequence.CompleteDurableAppend</c>, and each update replaces the event source's single entry, so once
    /// this batch lands the holder owns the current value and the released one is owned by nobody. The second event
    /// source should therefore be allowed to claim it. Flip this assertion to ShouldBeTrue when the accumulator is
    /// corrected to release a superseded claim.
    /// </summary>
    [Fact] void should_not_let_another_event_source_claim_the_released_value() => _otherClaimedTheReleasedValue.ShouldBeFalse();
}
