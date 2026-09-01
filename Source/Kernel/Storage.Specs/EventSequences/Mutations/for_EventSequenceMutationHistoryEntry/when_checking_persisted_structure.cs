// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationHistoryEntry;

public class when_checking_persisted_structure : Specification
{
    IEnumerable<(string Name, Type Type)> _properties;

    void Because() => _properties = typeof(EventSequenceMutationHistoryEntry).GetProperties().Select(_ => (_.Name, _.PropertyType));

    [Fact]
    void should_only_contain_terminal_receipt_fields() =>
        _properties.ShouldContainOnly(
            [
                (nameof(EventSequenceMutationHistoryEntry.Id), typeof(EventSequenceMutationId)),
                (nameof(EventSequenceMutationHistoryEntry.Ordinal), typeof(EventSequenceMutationOrdinal)),
                (nameof(EventSequenceMutationHistoryEntry.Origin), typeof(EventSequenceMutationOrigin)),
                (nameof(EventSequenceMutationHistoryEntry.Kind), typeof(EventSequenceMutationKind)),
                (nameof(EventSequenceMutationHistoryEntry.CommandHash), typeof(EventSequenceMutationCommandHash)),
                (nameof(EventSequenceMutationHistoryEntry.Target), typeof(EventSequenceMutationTarget)),
                (nameof(EventSequenceMutationHistoryEntry.RepairState), typeof(EventSequenceMutationRepairState))
            ]);
}
