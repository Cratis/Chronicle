// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.AspNetCore.Auditing;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_authenticated_requests;

public class when_using_explicit_middleware_after_authentication : given.a_tenant_request_host
{
    HttpStatusCode _status;

    async Task Establish() => await StartHost(automatic: false);
    async Task Because() => _status = await Send("/write", "tenant-a");

    [Fact] void should_succeed() => _status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_inject_the_claim_selected_namespace() => _endpointNamespaces.ToArray().ShouldEqual(["tenant-a"]);
    [Fact] void should_resolve_the_authenticated_namespace_once() => _resolutions.ToArray().ShouldEqual(["tenant-a"]);
    [Fact] void should_append_exactly_once() => _commits.ToArray().ShouldEqual(["tenant-a"]);
    [Fact] void should_complete_the_request_unit() => _units.Single().IsCompleted.ShouldBeTrue();
    [Fact] void should_remove_the_completion_feature() => _featuresAfterRequest.Single().ShouldBeFalse();
    [Fact] void should_install_causation_only_once() => _causation.Received(1).Add(CausationMiddleware.CausationType, Arg.Any<IDictionary<string, string>>());
}
