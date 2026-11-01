// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService.when_releasing;

public class when_strict_is_not_requested : given.a_compliance_service
{
    async Task Establish()
    {
        request.Strict = new Contracts.Compliance.ReleaseRequest().Strict;
        await storage.DeleteFor(event_store, event_store_namespace, identifier);
    }

    async Task Because() => result = await service.Release(request);

    [Fact] void should_keep_best_effort_behavior() => JsonNode.Parse(result.Payload)!["name"]!.GetValue<string>().ShouldBeEmpty();
    [Fact] void should_succeed() => result.HasError.ShouldBeFalse();
    [Fact] void should_have_no_failed_paths() => result.FailedProperties.ShouldBeEmpty();
    [Fact] void should_report_best_effort_mode() => result.Strict.ShouldBeFalse();
}
