// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_previewing;

public class and_inferred_scalars_use_all_of : given.an_inferred_composed_scalar_schema
{
    async Task Because() => await Preview();

    [Fact] void should_keep_the_wrapped_integer_value() => (JsonNode.Parse(_result.Value0.ReadModelEntries.Single())!["count"]?.GetValue<long>()).ShouldEqual(42L);
    [Fact] void should_keep_the_wrapped_reference_value() => (JsonNode.Parse(_result.Value0.ReadModelEntries.Single())!["code"]?.GetValue<string>()).ShouldEqual(Code.ToString());
    [Fact] void should_infer_the_effective_integer_format() => JsonSchema.FromJson(_result.Value0.ReadModel.Schema).Properties["count"].Format.ShouldEqual("int64");
    [Fact] void should_infer_the_effective_reference_format() => JsonSchema.FromJson(_result.Value0.ReadModel.Schema).Properties["code"].Format.ShouldEqual("guid");
    [Fact] void should_preserve_the_composed_schema() => JsonSchema.FromJson(_result.Value0.ReadModel.Schema).Properties["count"].AllOf.ShouldNotBeEmpty();
}
