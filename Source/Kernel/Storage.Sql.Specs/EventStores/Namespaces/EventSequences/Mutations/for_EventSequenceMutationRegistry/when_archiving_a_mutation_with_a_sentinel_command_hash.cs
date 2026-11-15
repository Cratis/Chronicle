// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_archiving_a_mutation_with_a_sentinel_command_hash : given.a_mutation_registry
{
    const string Sentinel = "sentinel-not-a-real-subject@example.invalid";

    List<string> _nonNullActiveColumns;
    List<string> _columnsContainingTheSentinel;
    EventSequenceMutationRegistryArchiveResult _archived;
    bool _sentinelWasStored;

    async Task Because()
    {
        var begin = await _registry.Begin(Request(_target, hash: Sentinel), _proposedTarget);
        _sentinelWasStored = (await HeadRow()).Any(_ => _.Value?.ToString()!.Contains(Sentinel) == true);
        var applying = await Apply(_registry, begin, EventSequenceMutationTransition.BeginApplying);
        var verifying = await _registry.Transition(_target, applying.Token!, EventSequenceMutationTransition.BeginVerifying);
        var committed = await _registry.Transition(_target, verifying.Token!, EventSequenceMutationTransition.CommitSourceWithoutRepair);
        _archived = await _registry.Archive(_target, committed.Token!);

        var row = await HeadRow();
        _nonNullActiveColumns = row.Where(_ => _.Key.StartsWith("Active", StringComparison.Ordinal) && _.Value is not null).Select(_ => _.Key).ToList();
        _columnsContainingTheSentinel = row.Where(_ => _.Value?.ToString()!.Contains(Sentinel) == true).Select(_ => _.Key).ToList();
    }

    [Fact] void should_have_stored_the_sentinel_while_the_mutation_was_active() => _sentinelWasStored.ShouldBeTrue();
    [Fact] void should_archive_the_mutation() => _archived.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.Archived);
    [Fact] void should_clear_every_active_column() => _nonNullActiveColumns.ShouldBeEmpty();
    [Fact] void should_leave_the_sentinel_in_no_head_column() => _columnsContainingTheSentinel.ShouldBeEmpty();

    async Task<Dictionary<string, object?>> HeadRow()
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = $"SELECT * FROM \"{WellKnownTableNames.EventSequenceMutationHeads}\"";
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).ShouldBeTrue();
        var row = new Dictionary<string, object?>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            row[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
        }

        return row;
    }
}
