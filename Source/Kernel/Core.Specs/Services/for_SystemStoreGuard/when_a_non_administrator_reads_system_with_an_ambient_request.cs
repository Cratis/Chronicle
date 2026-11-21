// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_a_non_administrator_reads_system_with_an_ambient_request : given.an_in_process_request
{
    [Theory]
    [MemberData(nameof(NonAdministrators))]
    public async Task should_refuse_the_system_store(string caller)
    {
        _httpContext.User = CallerNamed(caller);
        using var cancellation = new CancellationTokenSource();
        await Evaluate((CallContext)cancellation.Token);

        _allowed.ShouldBeFalse();
        Refusal.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    }
}
