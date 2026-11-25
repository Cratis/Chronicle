// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

public class when_stream_enforcement_is_disabled : Specification
{
    Services.given.an_entitled_caller _caller;
    readonly List<int> _items = [];
    bool _completed;
    bool _recorded;

    void Establish() => _caller = new(enforce: false);

    void Because()
    {
        _caller.Revoke();
        StoreCapabilityGuard.EndWhenRevoked(Observable.Return(42), _caller.Context, "store", "ns", StoreAccess.Read).Subscribe(_items.Add, () => _completed = true);
        _recorded = _caller.Context.ServerCallContext!.GetRecordedFailure() is not null;
    }

    void Destroy() => _caller.Dispose();

    [Fact] void should_deliver_the_item() => _items.ShouldEqual([42]);
    [Fact] void should_complete_normally() => _completed.ShouldBeTrue();
    [Fact] void should_record_no_failure() => _recorded.ShouldBeFalse();
}
