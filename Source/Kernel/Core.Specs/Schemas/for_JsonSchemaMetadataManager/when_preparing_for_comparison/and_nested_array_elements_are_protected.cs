// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_preparing_for_comparison;

public class and_nested_array_elements_are_protected : given.protected_nested_arrays
{
    JsonObject _masked;

    async Task Because() => _result = await _manager.TryPrepareForComparison(_schema, _plaintext, document =>
    {
        _masked = (JsonObject)document.DeepClone();
        return document;
    });

    [Fact] void should_restore_the_complete_nested_arrays() => JsonNode.DeepEquals(_result, _plaintext).ShouldBeTrue();
    [Fact] void should_keep_each_nested_array_opaque_during_conversion() => _masked["contacts"]![0]!.GetValueKind().ShouldEqual(System.Text.Json.JsonValueKind.String);
}
