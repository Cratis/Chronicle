// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService.when_releasing_strictly;

public class when_the_key_is_missing : given.a_compliance_service
{
    Task Establish() => storage.DeleteFor(event_store, event_store_namespace, identifier);

    async Task Because() => result = await service.Release(request);

    [Fact] void should_fail_the_release() => result.HasError.ShouldBeTrue();
    [Fact] void should_return_no_payload() => result.Payload.ShouldBeEmpty();
    [Fact] void should_identify_only_the_failed_path() => result.FailedProperties.ShouldContainOnly("name");
    [Fact] void should_report_strict_mode() => result.Strict.ShouldBeTrue();
    [Fact] void should_not_expose_the_value_in_the_error() => result.Error.Contains(SensitiveValue, StringComparison.Ordinal).ShouldBeFalse();
}
