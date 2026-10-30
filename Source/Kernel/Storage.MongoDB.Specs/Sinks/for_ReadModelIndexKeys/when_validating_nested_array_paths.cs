// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelIndexKeys;

public class when_validating_nested_array_paths : Specification
{
    JsonSchema _schema;

    async Task Establish() => _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"orders":{"type":"array","items":{"type":"object","properties":{"total":{"type":"number"},"lines":{"type":"array","items":{"type":"object","properties":{"sku":{"type":"string"}}}}}}}}}""");

    [Fact] void should_allow_a_single_nested_key() => ReadModelIndexKeys.Validate(new("orders.lines.sku") { Properties = [new("orders.lines.sku")] }, _schema);
    [Fact] void should_allow_outer_plus_inner_keys() => ReadModelIndexKeys.Validate(new("orders.total") { Properties = [new("orders.total"), new("orders.lines.sku")] }, _schema);
}
