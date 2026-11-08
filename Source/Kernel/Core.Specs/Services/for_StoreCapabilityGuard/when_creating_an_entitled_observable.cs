// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services.given;
using Grpc.Core;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

public class when_creating_an_entitled_observable
{
    [Fact]
    public async Task should_remain_lazy_and_never_create_a_refused_source()
    {
        using var caller = new an_entitled_caller();
        caller.Revoke();
        var created = 0;
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observable = SystemStoreGuard.Ensure(caller.Context, new QueryRouting("store", "ns"), StoreAccess.Read, () =>
        {
            created++;
            return Observable.Never<int>();
        });
        created.ShouldEqual(0);
        using var subscription = observable.Subscribe(_ => { }, exception => error.TrySetResult(exception));
        ((RpcException)await error.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System)).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        created.ShouldEqual(0);
    }

    [Fact]
    public void should_dispose_the_source_and_stop_observing_revocation()
    {
        using var caller = new an_entitled_caller();
        var disposed = false;
        Exception? error = null;
        var observable = SystemStoreGuard.Ensure(caller.Context, new QueryRouting("store", "ns"), StoreAccess.Read, () => Observable.Never<int>().Finally(() => disposed = true));
        var subscription = observable.Subscribe(_ => { }, exception => error = exception);
        subscription.Dispose();
        disposed.ShouldBeTrue();
        caller.Revoke();
        error.ShouldBeNull();
    }
}
