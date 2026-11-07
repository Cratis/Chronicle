// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services.given;
using Grpc.Core;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

public class when_access_is_withdrawn_before_monitoring
{
    [Theory]
    [InlineData(StoreAccess.Manage)]
    [InlineData(StoreAccess.Read)]
    public async Task should_refuse_an_admitted_stream_even_when_the_lease_keeps_renewing(StoreAccess remaining)
    {
        using var caller = new an_entitled_caller();
        using var source = new Subject<int>();
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        const StoreAccess access = StoreAccess.Read | StoreAccess.Manage;
        (await StoreCapabilityGuard.Permits(caller.Context, "store", "namespace", access)).ShouldBeTrue();
        var stream = StoreCapabilityGuard.EndWhenRevoked(source, caller.Context, "store", "namespace", access);
        caller.KeepOnly(remaining);
        using var subscription = stream.Subscribe(_ => { }, exception => error.TrySetResult(exception));
        for (var renewal = 0; renewal < 3; renewal++)
        {
            caller.Clock.Advance(TimeSpan.FromMinutes(4));
            await caller.ConfirmLease();
        }
        var exception = await error.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        ((RpcException)exception).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    }
}
