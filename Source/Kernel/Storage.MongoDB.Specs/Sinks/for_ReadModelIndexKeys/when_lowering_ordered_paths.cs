// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelIndexKeys;

public class when_lowering_ordered_paths : Specification
{
    JsonSchema _schema;
    IndexDefinition _compound;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string"},"details":{"type":"object","properties":{"total":{"type":"number"}}},"lines":{"type":"array","items":{"type":"object","properties":{"productId":{"type":"string"},"qty":{"type":"integer"}}}},"tags":{"type":"array","items":{"type":"string"}}}}""");
        _compound = new("name") { Properties = [new("name"), new("details.total", true)] };
    }

    [Fact] void should_keep_order_and_direction() => ReadModelIndexKeys.For(_compound).ShouldEqual(new BsonDocument { ["name"] = 1, ["details.total"] = -1 });
    [Fact] void should_keep_legacy_names() => ReadModelIndexKeys.Name(new("details.total")).ShouldEqual("chronicle_idx_details_total");
    [Fact] void should_name_single_descending_keys() => ReadModelIndexKeys.Name(new("name") { Properties = [new("name", true)] }).StartsWith("chronicle_idx_v2_", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_collide_with_a_legacy_suffix_path() => (ReadModelIndexKeys.Name(new("name") { Properties = [new("name", true)] }) == ReadModelIndexKeys.Name(new("name_desc"))).ShouldBeFalse();
    [Fact] void should_not_collapse_reversed_compound_keys() => ReadModelIndexKeys.For(_compound).Equals(ReadModelIndexKeys.For(new("details.total") { Properties = [new("details.total", true), new("name")] })).ShouldBeFalse();
    [Fact] void should_allow_one_multikey_path() => ReadModelIndexKeys.Validate(new("name") { Properties = [new("name"), new("lines.productId", true)] }, _schema);
    [Fact] void should_allow_keys_sharing_an_array() => ReadModelIndexKeys.Validate(new("lines.productId") { Properties = [new("lines.productId"), new("lines.qty")] }, _schema);
    [Fact] void should_reject_parallel_array_paths() => Catch.Exception(() => ReadModelIndexKeys.Validate(new("lines.productId") { Properties = [new("lines.productId"), new("tags")] }, _schema)).ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_hash_overlong_compound_names() => System.Text.Encoding.UTF8.GetByteCount(ReadModelIndexKeys.Name(new("name") { Properties = [new(new string('界', 80)), new("details.total")] })).ShouldBeLessThan(127);
}
