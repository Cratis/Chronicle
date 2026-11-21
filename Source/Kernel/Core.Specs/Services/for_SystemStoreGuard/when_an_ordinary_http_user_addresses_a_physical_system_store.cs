// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_an_ordinary_http_user_addresses_a_physical_system_store : given.an_in_process_request
{
    Exception? _exception;

    void Establish() => _httpContext.User = CallerNamed(OrdinaryUser);

    async Task Because()
    {
        BeginRequest();
        _exception = await Catch.Exception(() => SystemStoreGuard.Ensure(default, new QueryRouting("System+tenant", null)));
    }

    [Fact] void should_refuse_the_system_database() => ((RpcException)_exception!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
