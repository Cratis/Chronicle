// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_an_ambient_caller_lacks_ordinary_store_entitlements : given.an_in_process_request
{
    Exception? _exception;
    ServiceProvider _requestServices;

    protected override Authentication AuthenticationOptions => new() { EnforceStoreEntitlements = true };

    void Establish()
    {
        _httpContext.User = CallerNamed(OrdinaryUser);
        var decisions = _httpContext.RequestServices.GetRequiredService<IAuthorizationDecisions>();
        decisions.Decide(_httpContext.User, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForNamespace("shop", "tenant"))
            .Returns(AuthorizationDecision.Deny(AuthorizationReason.NotGranted, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForNamespace("shop", "tenant")));
        _requestServices = new ServiceCollection()
            .AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions { Authentication = AuthenticationOptions }))
            .AddSingleton(decisions)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .BuildServiceProvider();
        _httpContext.RequestServices = _requestServices;
    }

    async Task Because()
    {
        BeginRequest();
        _exception = await Catch.Exception(() => SystemStoreGuard.Ensure(default, new QueryRouting("shop", "tenant"), StoreAccess.Read, namespaceAddressed: true));
    }

    void Destroy() => _requestServices.Dispose();

    [Fact] void should_refuse_instead_of_treating_the_call_as_requestless() => ((RpcException)_exception!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
