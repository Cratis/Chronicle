// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_authenticated_requests;

public class when_using_automatic_middleware : given.a_tenant_request_host
{
    HttpStatusCode _first;
    HttpStatusCode _second;

    async Task Establish() => await StartHost();

    async Task Because()
    {
        _first = await Send("/write", "tenant-a");
        _second = await Send("/write", "tenant-b");
    }

    [Fact] void should_allow_the_first_tenant() => _first.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_allow_the_second_tenant() => _second.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_inject_the_claim_selected_namespace() => _endpointNamespaces.ToArray().ShouldEqual(["tenant-a", "tenant-b"]);
    [Fact] void should_resolve_only_authenticated_namespaces() => _resolutions.ToArray().ShouldEqual(["tenant-a", "tenant-b"]);
    [Fact] void should_commit_once_to_each_tenant() => _commits.ToArray().ShouldEqual(["tenant-a", "tenant-b"]);
    [Fact] void should_complete_both_request_units() => _units.All(_ => _.IsCompleted).ShouldBeTrue();
    [Fact] void should_create_separate_request_units() => ReferenceEquals(_units.First(), _units.Last()).ShouldBeFalse();
}
