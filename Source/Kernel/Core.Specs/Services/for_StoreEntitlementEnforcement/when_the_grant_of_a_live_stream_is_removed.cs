// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.Services.for_StoreEntitlementEnforcement;

public class when_the_grant_of_a_live_stream_is_removed : given.a_store_entitlement_caller
{
    Subject<int> _source;
    Subject<int> _auditedSource;
    TaskCompletionSource _enforcedSubscribed;
    TaskCompletionSource _auditedSubscribed;
    TaskCompletionSource _enforcedEnded;
    Exception? _enforcedError;
    bool _auditedCompleted;
    Exception? _auditedError;

    void Establish()
    {
        _granted = true;
        _source = new();
        _auditedSource = new();
        _enforcedSubscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _auditedSubscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _enforcedEnded = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        SystemStoreGuard.Ensure(ContextFor(true), new("shop", "default"), StoreAccess.Read, () => Signalling(_source, _enforcedSubscribed)).Subscribe(
            _ => { },
            error =>
            {
                _enforcedError = error;
                _enforcedEnded.TrySetResult();
            });
        SystemStoreGuard.Ensure(ContextFor(false), new("shop", "default"), StoreAccess.Read, () => Signalling(_auditedSource, _auditedSubscribed)).Subscribe(_ => { }, error => _auditedError = error, () => _auditedCompleted = true);
        await Task.WhenAll(_enforcedSubscribed.Task, _auditedSubscribed.Task).WaitAsync(TimeSpan.FromSeconds(10));
        _granted = false;
        _changes.OnNext(default);
        await _enforcedEnded.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    static IObservable<int> Signalling(IObservable<int> source, TaskCompletionSource subscribed) => Observable.Create<int>(observer =>
    {
        var subscription = source.Subscribe(observer);
        subscribed.TrySetResult();
        return subscription;
    });

    [Fact] void should_end_the_enforced_stream_with_permission_denied() => ((RpcException)_enforcedError).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_end_the_audited_stream() => (_auditedError is null && !_auditedCompleted).ShouldBeTrue();
}
