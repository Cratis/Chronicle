// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_query_uses_its_execution_services : given.a_query_operation_policy
{
    bool _requestDenied;
    bool _requestGranted;
    IAdministratorAuthorization _requestAuthorization;

    void Establish()
    {
        _enforce = true;
        _authorization.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(true);
        _requestAuthorization = Substitute.For<IAdministratorAuthorization>();
        _services.GetService(typeof(IAdministratorAuthorization)).Returns(_requestAuthorization);
    }

    async Task Because()
    {
        _requestDenied = await Allows(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "System" });
        _authorization.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(false);
        _requestAuthorization.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(true);
        _requestGranted = await Allows(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "System" });
    }

    [Fact] void should_not_borrow_privilege_from_the_ambient_http_request() => _requestDenied.ShouldBeFalse();
    [Fact] void should_use_the_query_execution_authorizer() => _requestGranted.ShouldBeTrue();
}
