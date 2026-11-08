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
                (nameof(EventSequenceMutationHistoryEntry.Kind), typeof(EventSequenceMutationKind)),
                (nameof(EventSequenceMutationHistoryEntry.Target), typeof(EventSequenceMutationTarget)),
                (nameof(EventSequenceMutationHistoryEntry.TerminalPhase), typeof(EventSequenceMutationPhase)),
                (nameof(EventSequenceMutationHistoryEntry.RepairState), typeof(EventSequenceMutationRepairState)),
                (nameof(EventSequenceMutationHistoryEntry.TerminalWitness), typeof(EventSequenceMutationTerminalWitness)),
                (nameof(EventSequenceMutationHistoryEntry.ArchivedAt), typeof(DateTimeOffset))
            ]);
    [Fact] void should_not_contain_payload() => HasPropertyContaining("payload").ShouldBeFalse();
    [Fact] void should_not_contain_audit_data() => HasPropertyContaining("audit").ShouldBeFalse();
    [Fact] void should_not_contain_content() => HasPropertyContaining("content").ShouldBeFalse();
    [Fact] void should_not_contain_schema() => HasPropertyContaining("schema").ShouldBeFalse();
    [Fact] void should_not_contain_origin() => HasPropertyContaining("origin").ShouldBeFalse();
    [Fact] void should_not_contain_command_hash() => HasPropertyContaining("command").ShouldBeFalse();
    [Fact] void should_not_contain_reason() => HasPropertyContaining("reason").ShouldBeFalse();
    [Fact] void should_not_contain_event_source() => HasPropertyContaining("eventsource").ShouldBeFalse();
    [Fact] void should_not_contain_selector() => HasPropertyContaining("selector").ShouldBeFalse();

    bool HasPropertyContaining(string value) => _properties.Any(_ => _.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
}
