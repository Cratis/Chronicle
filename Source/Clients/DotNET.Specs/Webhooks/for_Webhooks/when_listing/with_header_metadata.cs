// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.when_listing;

public class with_header_metadata : given.webhook_query_responses
{
    void Establish()
    {
        _response.Headers = new Dictionary<string, string> { ["x-key"] = "synthetic-legacy-value", ["x-empty"] = "synthetic-stale-value" };
        _response.HeaderValuesSet = new Dictionary<string, bool> { ["x-key"] = true, ["x-empty"] = false };
    }

    async Task Because() => _definition = (await _webhooks.GetAll()).Single();

    [Fact] void should_keep_the_names() => _definition.Target.Headers.Keys.ShouldContainOnly("x-key", "x-empty");
    [Fact] void should_discard_legacy_values() => _definition.Target.Headers.Values.All(_ => _.Length == 0).ShouldBeTrue();
    [Fact] void should_copy_the_presence_metadata() => _definition.Target.HeaderValuesSet["x-key"].ShouldBeTrue();
    [Fact] void should_copy_the_empty_value_metadata() => _definition.Target.HeaderValuesSet["x-empty"].ShouldBeFalse();
    [Fact] void should_not_expose_values_in_json() => JsonSerializer.Serialize(new { _definition.Target.Headers, _definition.Target.HeaderValuesSet }).ShouldNotContain("synthetic-legacy-value");
}
