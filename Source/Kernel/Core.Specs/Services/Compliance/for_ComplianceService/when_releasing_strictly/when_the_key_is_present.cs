// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService.when_releasing_strictly;

public class when_the_key_is_present : given.a_compliance_service
{
    async Task Because() => result = await service.Release(request);

    [Fact] void should_release_the_original_value() => JsonNode.Parse(result.Payload)!["name"]!.GetValue<string>().ShouldEqual(SensitiveValue);
    [Fact] void should_succeed() => result.HasError.ShouldBeFalse();
    [Fact] void should_have_no_failed_paths() => result.FailedProperties.ShouldBeEmpty();
    [Fact] void should_report_strict_mode() => result.Strict.ShouldBeTrue();
}
