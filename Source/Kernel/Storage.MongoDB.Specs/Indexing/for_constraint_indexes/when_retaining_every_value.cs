// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_constraint_indexes;

[Collection(MongoDBCollection.Name)]
public class when_retaining_every_value(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    UniqueConstraintsStorage _storage;
    UniqueConstraintDefinition _definition;
    readonly EventSourceId _owner = EventSourceId.New();
    readonly EventSourceId _otherOwner = EventSourceId.New();

    async Task Establish()
    {
        _storage = new(_database, EventSequenceId.Log, Substitute.For<ILogger<UniqueConstraintsStorage>>());
        _definition = new("versions", []) { Mode = UniqueConstraintMode.PerValue };
        await _storage.Save(_owner, _definition, 42L, "first");
    }

    async Task Because() => await _storage.Save(_owner, _definition, 43L, "second");

    [Fact] async Task should_keep_the_earlier_value_reserved() => (await _storage.IsAllowed(_otherOwner, _definition, "first")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_keep_the_new_value_reserved() => (await _storage.IsAllowed(_otherOwner, _definition, "second")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_allow_the_owner_to_reclaim_its_earlier_value() => (await _storage.IsAllowed(_owner, _definition, "first")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_preserve_the_original_claim_position() => (await _storage.IsAllowed(_otherOwner, _definition, "first")).SequenceNumber.ShouldEqual((EventSequenceNumber)42L);
}
