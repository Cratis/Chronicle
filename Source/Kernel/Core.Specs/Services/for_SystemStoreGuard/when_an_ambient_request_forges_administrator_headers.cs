// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_an_ambient_request_forges_administrator_headers : given.an_in_process_request
{
    void Establish()
    {
        _httpContext.User = CallerNamed(Anonymous);
        _httpContext.Request.Headers["x-ms-client-principal-name"] = "admin";
        _httpContext.Request.Headers["x-ms-client-principal-groups"] = "administrators";
        _httpContext.Request.Headers["x-cratis-roles"] = "admin";
    }

    async Task Because() => await Evaluate();

    [Fact] void should_not_allow_the_caller() => _allowed.ShouldBeFalse();
    [Fact] void should_refuse_the_system_store() => Refusal.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
