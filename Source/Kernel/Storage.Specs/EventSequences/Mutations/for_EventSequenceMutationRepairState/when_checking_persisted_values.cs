// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationRepairState;

public class when_checking_persisted_values : Specification
{
    [Fact] void should_persist_unspecified_as_zero() => ((int)EventSequenceMutationRepairState.Unspecified).ShouldEqual(0);
    [Fact] void should_persist_not_required_as_one() => ((int)EventSequenceMutationRepairState.NotRequired).ShouldEqual(1);
    [Fact] void should_persist_pending_as_two() => ((int)EventSequenceMutationRepairState.Pending).ShouldEqual(2);
    [Fact] void should_persist_dispatching_as_three() => ((int)EventSequenceMutationRepairState.Dispatching).ShouldEqual(3);
    [Fact] void should_persist_accepted_as_four() => ((int)EventSequenceMutationRepairState.Accepted).ShouldEqual(4);
    [Fact] void should_persist_unknown_as_five() => ((int)EventSequenceMutationRepairState.Unknown).ShouldEqual(5);
}
