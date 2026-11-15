// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_saving;

[PerFactSpecificationLifecycle] // Facts run their own asynchronous operations, so each fact gets its own context.
public class and_the_value_table_cannot_collide_with_a_legacy_constraint_table : given.a_per_value_constraint
{
    async Task Because() => await _storage.Save(_owner, _definition, 42L, "first");

    [Fact] async Task should_use_a_suffix_distinct_from_legacy_tables() => await _database.Received(1).UniqueConstraintValuesTable((EventStoreName)"store", (EventStoreNamespaceName)"namespace", $"{EventSequenceId.Log}_versions_constraint_values");
}
