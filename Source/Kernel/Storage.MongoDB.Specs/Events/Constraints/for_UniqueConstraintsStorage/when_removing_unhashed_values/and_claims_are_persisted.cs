// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorage.when_removing_unhashed_values;

[Collection(MongoDBCollection.Name)]
public class and_claims_are_persisted(MongoDBFixture fixture) : given.a_unique_constraints_storage(fixture)
{
    UniqueConstraintDefinition _retained;
    (long PerEventSource, long PerValue) _deleted;
    readonly string _hashShapedRawValue = new('a', 64);

    async Task Establish()
    {
        _retained = _definition with { Mode = UniqueConstraintMode.PerValue };
        var values = new[] { _hashShapedRawValue, new string('A', 64), "personal-value", new string('a', 63), _hashShapedRawValue + "\n" };
        for (var index = 0; index < values.Length; index++)
        {
            await _storage.Save($"owner-{index}", _definition, 42, values[index]);
            await _storage.Save($"owner-{index}", _retained, 42, values[index], "scope");
        }
    }

    async Task Because() => _deleted = await _storage.RemoveUnhashedValues();

    [Fact] void should_delete_all_unhashed_source_claims() => _deleted.PerEventSource.ShouldEqual(4L);
    [Fact] void should_delete_all_unhashed_retained_claims() => _deleted.PerValue.ShouldEqual(4L);
    [Fact] async Task should_keep_the_hash_shaped_source_claim() => (await _storage.GetFor("owner-0", _definition)).Select(value => value.Value).ShouldContainOnly(_hashShapedRawValue);
    [Fact] async Task should_keep_the_hash_shaped_retained_claim() => (await _storage.GetFor("owner-0", _retained, "scope")).Select(value => value.Value).ShouldContainOnly(_hashShapedRawValue);
    [Fact] async Task should_be_idempotent() => (await _storage.RemoveUnhashedValues()).ShouldEqual((0L, 0L));
}
