// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_authenticated_requests;

public class when_a_tenant_request_returns_a_failure_status : given.a_tenant_request_host
{
    HttpStatusCode _status;

    async Task Establish() => await StartHost(automatic: false);
    async Task Because() => _status = await Send("/failure", "tenant-a");

    [Fact] void should_preserve_the_failure_status() => _status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_resolve_the_authenticated_namespace() => _resolutions.ToArray().ShouldEqual(["tenant-a"]);
    [Fact] void should_commit_to_the_authenticated_namespace() => _commits.ToArray().ShouldEqual(["tenant-a"]);
    [Fact] void should_complete_the_unit() => _units.Single().IsCompleted.ShouldBeTrue();
    [Fact] void should_remove_the_completion_feature() => _featuresAfterRequest.Single().ShouldBeFalse();
}
