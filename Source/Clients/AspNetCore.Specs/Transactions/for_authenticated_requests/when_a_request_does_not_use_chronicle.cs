// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_authenticated_requests;

public class when_a_request_does_not_use_chronicle : given.a_tenant_request_host
{
    HttpStatusCode _status;

    async Task Establish() => await StartHost();
    async Task Because() => _status = await Send("/empty");

    [Fact] void should_succeed_without_authentication() => _status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_not_resolve_a_namespace() => _resolutions.ShouldBeEmpty();
    [Fact] void should_not_append_events() => _commits.ShouldBeEmpty();
}
