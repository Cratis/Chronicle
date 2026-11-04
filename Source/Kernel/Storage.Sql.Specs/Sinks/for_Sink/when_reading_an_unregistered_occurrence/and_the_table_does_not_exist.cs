// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_an_unregistered_occurrence;

/// <summary>
/// The occurrence is shaped like a revert container but the read model never had it. Reading must not create it.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISqlSinkHarness"/> supplying the provider under specification.</typeparam>
public abstract class and_the_table_does_not_exist<THarness> : given.a_read_model_with_a_canary<THarness>
    where THarness : ISqlSinkHarness, new()
{
    const string Occurrence = "ghost-20240101000000-deadbeef";

    protected bool _created;

    async Task Because()
    {
        await ReadOccurrence(Occurrence);
        _created = await TableExists(Occurrence);
    }
}
