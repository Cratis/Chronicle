// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_authenticated_requests;

public class when_an_unauthenticated_request_is_rejected : given.a_tenant_request_host
{
    HttpStatusCode _status;

    async Task Establish() => await StartHost();
    async Task Because() => _status = await Send("/write");

    [Fact] void should_challenge_the_request() => _status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_resolve_a_namespace() => _resolutions.ShouldBeEmpty();
    [Fact] void should_not_append_events() => _commits.ShouldBeEmpty();
    [Fact] void should_not_enter_the_endpoint() => _units.ShouldBeEmpty();
}
