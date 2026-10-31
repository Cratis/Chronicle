// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_constraint_indexes;

[Collection(MongoDBCollection.Name)]
public class when_claiming_a_retained_value_concurrently(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    UniqueConstraintsStorage _storage;
    UniqueConstraintDefinition _definition;
    Exception?[] _errors;

    void Establish()
    {
        _storage = new(_database, EventSequenceId.Log, Substitute.For<ILogger<UniqueConstraintsStorage>>());
        _definition = new("versions", []) { Mode = UniqueConstraintMode.PerValue };
    }

    async Task Because() => _errors = await Task.WhenAll(
        Catch.Exception(() => _storage.Save(EventSourceId.New(), _definition, 42L, "shared")),
        Catch.Exception(() => _storage.Save(EventSourceId.New(), _definition, 43L, "shared")));

    [Fact] void should_accept_one_claim() => _errors.Count(_ => _ is null).ShouldEqual(1);
    [Fact] void should_reject_the_other_claim_as_a_constraint_collision() => _errors.Single(_ => _ is not null).ShouldBeOfExactType<DuplicateUniqueConstraintValue>();
}
