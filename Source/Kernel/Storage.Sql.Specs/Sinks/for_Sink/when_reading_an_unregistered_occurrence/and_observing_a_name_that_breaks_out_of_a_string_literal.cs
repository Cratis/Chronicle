// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_an_unregistered_occurrence;

/// <summary>
/// The occurrence of an observed read closes the string literal of a catalog lookup and appends a statement dropping another table.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISqlSinkHarness"/> supplying the provider under specification.</typeparam>
public abstract class and_observing_a_name_that_breaks_out_of_a_string_literal<THarness> : given.a_read_model_with_a_canary<THarness>
    where THarness : ISqlSinkHarness, new()
{
    const string Occurrence = "x'; DROP TABLE canary; --";

    protected bool _created;

    async Task Because()
    {
        await ObserveOccurrence(Occurrence);
        _created = await TableExists(Occurrence);
    }
}
