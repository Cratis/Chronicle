// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public abstract class and_the_event_has_a_single_write_of_a_single_join<THarness> : and_the_event_has_a_single_write<THarness>
    where THarness : ISqlSinkHarness
{
    protected override Task Seed() => Apply("target", 1, Set(("joinId", "J")));

    protected override Task Write() => Apply("trigger", 2, KeyedFromJoin("joinId", "J", ("x", 7)));
}
