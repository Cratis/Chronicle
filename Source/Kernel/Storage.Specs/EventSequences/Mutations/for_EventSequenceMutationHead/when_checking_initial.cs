// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationHead;

public class when_checking_initial : Specification
{
    [Fact] void should_have_untracked_coverage() => EventSequenceMutationHead.Initial.Coverage.ShouldEqual(EventSequenceMutationCoverage.Untracked);
    [Fact] void should_have_no_assigned_ordinal() => EventSequenceMutationHead.Initial.LastAssignedOrdinal.ShouldEqual(EventSequenceMutationOrdinal.NotSet);
    [Fact] void should_have_no_active_mutation() => EventSequenceMutationHead.Initial.Active.ShouldBeNull();
}
