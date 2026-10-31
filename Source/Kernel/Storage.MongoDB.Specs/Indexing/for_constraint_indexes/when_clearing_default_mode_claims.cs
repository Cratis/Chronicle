// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_constraint_indexes;

[Collection(MongoDBCollection.Name)]
public class when_clearing_default_mode_claims(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    UniqueConstraintsStorage _storage;
    UniqueConstraintDefinition _definition;

    async Task Establish()
    {
        _storage = new(_database, EventSequenceId.Log, Substitute.For<ILogger<UniqueConstraintsStorage>>());
        _definition = new("unique", []);
        await _storage.Save("first", _definition, 1UL, "first-value", "selected");
        await _storage.Save("second", _definition, 2UL, "second-value", "selected");
        await _storage.Save("first", _definition, 3UL, "other-scope", "other");
        await _storage.Save("first", _definition with { Name = "other-constraint" }, 4UL, "other-constraint", "selected");
    }

    async Task Because() => await _storage.ClearValues(_definition, "selected");

    [Fact] async Task should_release_the_first_owners_claim() => (await _storage.IsAllowed("other", _definition, "first-value", "selected")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_release_the_second_owners_claim() => (await _storage.IsAllowed("other", _definition, "second-value", "selected")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_preserve_another_scope() => (await _storage.IsAllowed("other", _definition, "other-scope", "other")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_preserve_another_constraint() => (await _storage.IsAllowed("other", _definition with { Name = "other-constraint" }, "other-constraint", "selected")).IsAllowed.ShouldBeFalse();
}
