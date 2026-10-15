// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_strictly_releasing;

public class and_nested_array_elements_are_protected : given.protected_nested_arrays
{
    async Task Because() => _result = await _manager.TryRelease("store", "tenant", _schema, "subject", _protected);

    [Fact] void should_recover_the_complete_nested_arrays() => JsonNode.DeepEquals(_result, _plaintext).ShouldBeTrue();
    [Fact] void should_have_protected_each_nested_array_as_a_whole() => _protected["contacts"]![0]!.GetValueKind().ShouldEqual(System.Text.Json.JsonValueKind.String);
}
