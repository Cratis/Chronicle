// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_saving;

public class and_two_sources_claim_the_same_retained_value : given.a_per_value_constraint
{
    Exception?[] _errors;

    async Task Because() => _errors = await Task.WhenAll(
        Catch.Exception(() => _storage.Save(_owner, _definition, 42L, "shared")),
        Catch.Exception(() => _storage.Save(_otherOwner, _definition, 43L, "shared")));

    [Fact] void should_accept_one_claim() => _errors.Count(_ => _ is null).ShouldEqual(1);
    [Fact] void should_refuse_the_other_claim_as_a_constraint_collision() => _errors.Single(_ => _ is not null).ShouldBeOfExactType<DuplicateUniqueConstraintValue>();
}
