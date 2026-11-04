// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public abstract class and_the_event_has_a_single_write_of_a_guarded_direct_update<THarness> : and_the_event_has_a_single_write<THarness>
    where THarness : ISqlSinkHarness
{
    protected override Task Seed() => Apply("row", 1, Set(("group", "first")));

    protected override Task Write() => Apply("row", 5, SinkWriteMode.GuardedUpsert, Set(("group", "second")));
}
