// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using FactAttribute = Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given.sqlite.FactAttribute;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_checking_many_claims;

public class and_values_are_held_free_or_repeated : given.a_batch_storage
{
    async Task Because()
    {
        await Check(UniqueConstraintMode.PerValue);
        await Check(UniqueConstraintMode.PerEventSource);
    }

    [Fact] void should_match_checks_in_both_modes() => VerifyClaims();
    [Fact] void should_accept_empty_changes_in_both_modes() => VerifyChanges();

    async Task Check(UniqueConstraintMode mode)
    {
        await Prepare(mode);
        await Seed();
        await CheckClaims([new("owner", "held"), new("other", "held"), new("other", "free"), new("owner", "free"), new("moving", "old")]);
        await CheckClaims([]);
        await CheckChanges([]);
    }
}
