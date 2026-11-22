// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.when_listing;

public class without_header_metadata : given.webhook_query_responses
{
    void Establish() => _response.Headers = new Dictionary<string, string> { ["x-key"] = "synthetic-legacy-value", ["x-empty"] = string.Empty };

    async Task Because() => _definition = (await _webhooks.GetAll()).Single();

    [Fact] void should_keep_the_legacy_name() => _definition.Target.Headers.Keys.ShouldContainOnly("x-key", "x-empty");
    [Fact] void should_discard_the_legacy_value() => _definition.Target.Headers["x-key"].ShouldEqual(string.Empty);
    [Fact] void should_report_legacy_value_presence_without_retaining_the_value() => _definition.Target.HeaderValuesSet["x-key"].ShouldBeTrue();
    [Fact] void should_report_legacy_empty_value_as_unset() => _definition.Target.HeaderValuesSet["x-empty"].ShouldBeFalse();
}
