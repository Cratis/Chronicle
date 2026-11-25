// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

public class when_stream_access_is_refused : Specification
{
    Services.given.an_entitled_caller _caller;
    Exception _error;
    Exception _recordedDuringError;

    void Establish() => _caller = new();

    void Because()
    {
        _caller.Revoke();
        StoreCapabilityGuard.EndWhenRevoked(Observable.Never<int>(), _caller.Context, "store", "ns", StoreAccess.Read).Subscribe(
            _ => { },
            error =>
            {
                _error = error;
                _recordedDuringError = _caller.Context.ServerCallContext!.GetRecordedFailure();
            });
    }

    void Destroy() => _caller.Dispose();

    [Fact] void should_refuse_the_subscription() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_record_the_failure_before_notifying_the_observer() => _recordedDuringError.ShouldEqual(_error);
}
