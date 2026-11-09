// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Services.given;
using Cratis.Chronicle.Storage;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Occurrences.for_Occurrences;

public class when_an_observable_query_loses_access
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task should_end_the_real_query_with_permission_denied(bool expireLease, bool confirmLease)
    {
        using var caller = new an_entitled_caller();
        var storage = Substitute.For<IStorage>();
        var service = new Occurrences(Substitute.For<ICommandPipeline>(), storage, NamespaceCasingContext.Create(), NullLogger<Occurrences>.Instance);
        var initial = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = 0;
        using var subscription = service.ObserveOccurrences(new ObserveOccurrencesRequest { EventStore = "store", Namespace = "ns", Trigger = "trigger" }, caller.Context)
            .Subscribe(
            _ =>
            {
                received++;
                initial.TrySetResult();
            },
            error => failed.TrySetResult(error));
        await initial.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        if (confirmLease)
        {
            caller.Clock.Advance(TimeSpan.FromMinutes(3));
            await caller.ConfirmLease();
            caller.Clock.Advance(TimeSpan.FromMinutes(3));
            failed.Task.IsCompleted.ShouldBeFalse();
        }
        if (expireLease) caller.Clock.Advance(TimeSpan.FromMinutes(6));
        else caller.Revoke();
        var error = await failed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        ((RpcException)error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        var atRefusal = received;
        caller.Clock.Advance(TimeSpan.FromMinutes(6));
        caller.Revoke();
        received.ShouldEqual(atRefusal);
    }

    [Fact]
    public async Task should_keep_an_audit_only_query_open()
    {
        using var caller = new an_entitled_caller(enforce: false);
        var service = new Occurrences(Substitute.For<ICommandPipeline>(), Substitute.For<IStorage>(), NamespaceCasingContext.Create(), NullLogger<Occurrences>.Instance);
        var initial = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception? error = null;
        using var subscription = service.ObserveOccurrences(new ObserveOccurrencesRequest { EventStore = "store", Namespace = "ns", Trigger = "trigger" }, caller.Context)
            .Subscribe(_ => initial.TrySetResult(), failure => error = failure);
        await initial.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        caller.Revoke();
        caller.Clock.Advance(TimeSpan.FromMinutes(6));
        error.ShouldBeNull();
    }
}
