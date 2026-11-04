// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public abstract class and_the_event_has_a_single_write_of_a_single_child_removal<THarness> : and_the_event_has_a_single_write<THarness>
    where THarness : ISqlSinkHarness
{
    protected override Task Seed() => Apply("row", 1, AddChild("c1"));

    protected override Task Write() => Apply("row", 2, RemoveChildFromAll("c1"));
}
