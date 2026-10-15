// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_preparing_for_comparison;

public class and_additional_properties_are_protected : given.protected_additional_properties
{
    JsonObject _masked;

    async Task Because() => _result = await _manager.TryPrepareForComparison(_schema, _plaintext, document =>
    {
        _masked = (JsonObject)document.DeepClone();
        return document;
    });

    [Fact] void should_restore_every_dynamic_property() => JsonNode.DeepEquals(_result, _plaintext).ShouldBeTrue();
    [Fact] void should_keep_the_dynamic_values_opaque_during_conversion() => _masked["contacts"]!["home"]!.GetValue<string>().ShouldNotEqual("001");
}
