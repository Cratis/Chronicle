// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService.when_releasing_strictly;

public class when_the_ciphertext_is_tampered : given.a_compliance_service
{
    void Establish()
    {
        var content = JsonNode.Parse(request.Payload)!.AsObject();
        var encrypted = Convert.FromBase64String(content["name"]!.GetValue<string>());
        encrypted[^1] ^= 1;
        content["name"] = Convert.ToBase64String(encrypted);
        request.Payload = content.ToJsonString();
    }

    async Task Because() => result = await service.Release(request);

    [Fact] void should_fail_the_release() => result.HasError.ShouldBeTrue();
    [Fact] void should_return_no_payload() => result.Payload.ShouldBeEmpty();
    [Fact] void should_identify_only_the_failed_path() => result.FailedProperties.ShouldContainOnly("name");
    [Fact] void should_report_strict_mode() => result.Strict.ShouldBeTrue();
    [Fact] void should_not_expose_the_ciphertext_in_the_error() => result.Error.Contains(JsonNode.Parse(request.Payload)!["name"]!.GetValue<string>(), StringComparison.Ordinal).ShouldBeFalse();
}
