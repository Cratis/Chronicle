// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public abstract class and_the_event_has_a_single_write_of_a_removal<THarness> : and_the_event_has_a_single_write<THarness>
    where THarness : ISqlSinkHarness
{
    protected override Task Seed() => Apply("row", 1, Set(("group", "first")));

    protected override Task Write() => Apply("row", 2, new Removed(new ExpandoObject()));
}
