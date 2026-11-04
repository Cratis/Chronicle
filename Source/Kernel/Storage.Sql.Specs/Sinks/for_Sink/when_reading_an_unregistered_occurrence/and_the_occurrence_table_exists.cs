// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_an_unregistered_occurrence;

/// <summary>
/// The occurrence is a table the read model really has, such as the backup a replay leaves behind. It stays readable.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISqlSinkHarness"/> supplying the provider under specification.</typeparam>
public abstract class and_the_occurrence_table_exists<THarness> : given.a_read_model_with_a_canary<THarness>
    where THarness : ISqlSinkHarness, new()
{
    const string Occurrence = "test_read_models-20240101000000-deadbeef";

    async Task Establish()
    {
        var occurrenceSink = _sqlHarness.CreateSink(CreateReadModelDefinition() with { ContainerName = Occurrence });
        await occurrenceSink.ApplyChanges(_key, ChangesetSettingCountTo(7), 1UL);
    }

    async Task Because() => await ReadOccurrence(Occurrence);
}
