// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_strictly_releasing;

public class and_additional_properties_are_protected : given.protected_additional_properties
{
    async Task Because() => _result = await _manager.TryRelease("store", "tenant", _schema, "subject", _protected);

    [Fact] void should_recover_every_dynamic_property() => JsonNode.DeepEquals(_result, _plaintext).ShouldBeTrue();
    [Fact] void should_have_protected_the_dynamic_values() => JsonNode.DeepEquals(_protected, _plaintext).ShouldBeFalse();
}
