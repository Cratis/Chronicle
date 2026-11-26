// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_an_in_process_http_request_reads_an_ordinary_store : given.an_in_process_request
{
    IAuthorizationDecisions _decisions;
    bool _permitted;

    void Establish() => _decisions = _httpContext.RequestServices.GetRequiredService<IAuthorizationDecisions>();

    async Task Because()
    {
        BeginRequest();
        _permitted = await StoreCapabilityGuard.Permits(default, new QueryRouting("shop", "tenant"));
    }

    [Fact] void should_permit_the_entitled_caller() => _permitted.ShouldBeTrue();
    [Fact] async Task should_decide_for_the_ambient_principal_and_requested_scope() => await _decisions.Received(1).Decide(
        _httpContext.User, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForNamespace("shop", "tenant"));
}
