// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_previewing;

public class and_an_inferred_scalar_uses_nullable_any_of : given.an_inferred_composed_scalar_schema
{
    async Task Because() => await Preview();

    [Fact] void should_keep_the_non_null_boolean_value() => (JsonNode.Parse(_result.Value0.ReadModelEntries.Single())!["enabled"]?.GetValue<bool>()).ShouldEqual(true);
    [Fact] void should_infer_the_effective_boolean_type() => JsonSchema.FromJson(_result.Value0.ReadModel.Schema).Properties["enabled"].Type.ShouldEqual(JsonObjectType.Boolean | JsonObjectType.Null);
    [Fact] void should_preserve_the_null_branch() => JsonSchema.FromJson(_result.Value0.ReadModel.Schema).Properties["enabled"].AnyOf.Any(branch => branch.Type == JsonObjectType.Null).ShouldBeTrue();
}
