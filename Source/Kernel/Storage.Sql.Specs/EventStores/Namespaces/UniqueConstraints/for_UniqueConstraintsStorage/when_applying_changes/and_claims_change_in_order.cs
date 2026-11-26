// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using FactAttribute = Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given.sqlite.FactAttribute;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_applying_changes;

public class and_claims_change_in_order : given.a_batch_storage
{
    async Task Because()
    {
        await Check(UniqueConstraintMode.PerValue);
        await Check(UniqueConstraintMode.PerEventSource);
    }

    [Fact] void should_match_ordered_changes_and_removals_in_both_modes() => VerifyChanges();

    async Task Check(UniqueConstraintMode mode)
    {
        await Prepare(mode);
        await Seed();
        await CheckChanges([
            Save("new", "new-value"),
            Save("owner", "held", 30, subject: "updated"),
            Save("moving", "moved", 31, subject: "mover"),
            Save("intruder", "held"),
            Save("first", "duplicate"),
            Save("second", "duplicate"),
            Remove("released"),
            Save("replacement", "release"),
            Remove("new"),
            Save("new-again", "new-value"),
            Save("scoped", "held", scope: "other"),
            Save("scoped-duplicate", "held", scope: "other"),
            Save("owner", "held", 32),
            Remove("missing"),
            Save("temporary", "temporary"),
            Remove("temporary"),
            Save("temporary", "final")]);
        await CheckChanges([
            RemoveValue("intruder", "held"),
            RemoveValue("owner", "held"),
            Save("replacement", "held"),
            Save("multiple", "one"),
            Save("multiple", "two"),
            RemoveValue("multiple", "one"),
            Remove("multiple"),
            Save("next", "two")]);
    }
}
