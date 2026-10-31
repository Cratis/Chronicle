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
public class when_releasing_one_retained_value(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
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
        await _storage.Save(_owner, _definition, 43L, "second");
        await _storage.Save(_otherOwner, _definition, 44L, "foreign");
    }

    async Task Because()
    {
        await _storage.RemoveValue(_owner, _definition, "first");
        await _storage.RemoveValue(_owner, _definition, "foreign");
    }

    [Fact] async Task should_release_the_selected_value() => (await _storage.IsAllowed(_otherOwner, _definition, "first")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_keep_the_other_value() => (await _storage.IsAllowed(_otherOwner, _definition, "second")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_not_release_another_sources_value() => (await _storage.IsAllowed(_owner, _definition, "foreign")).IsAllowed.ShouldBeFalse();
}
