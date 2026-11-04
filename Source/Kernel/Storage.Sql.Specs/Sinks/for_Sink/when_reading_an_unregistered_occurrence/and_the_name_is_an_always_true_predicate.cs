// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_an_unregistered_occurrence;

/// <summary>
/// The occurrence turns the catalog lookup into an always true predicate, so the read model appears to have that table.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISqlSinkHarness"/> supplying the provider under specification.</typeparam>
public abstract class and_the_name_is_an_always_true_predicate<THarness> : given.a_read_model_with_a_canary<THarness>
    where THarness : ISqlSinkHarness, new()
{
    const string Occurrence = "nope' OR '1'='1";

    protected bool _created;

    async Task Because()
    {
        await ReadOccurrence(Occurrence);
        _created = await TableExists(Occurrence);
    }
}
