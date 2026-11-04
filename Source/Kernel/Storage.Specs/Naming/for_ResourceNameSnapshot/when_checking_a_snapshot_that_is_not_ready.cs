// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameSnapshot;

public class when_checking_a_snapshot_that_is_not_ready : Specification
{
    ResourceNameSnapshot _snapshot;
    ResourceNameStatus _store;
    ResourceNameStatus _system;
    bool _readyAfterPublishing;

    void Establish() => _snapshot = ResourceNameSnapshot.NotReady();

    void Because()
    {
        _store = _snapshot.Check("orders");
        _system = _snapshot.Check("System");
        _snapshot.Publish([], []);
        _readyAfterPublishing = _snapshot.IsReady;
    }

    [Fact] void should_not_be_ready() => ResourceNameSnapshot.NotReady().IsReady.ShouldBeFalse();
    [Fact] void should_report_not_ready_for_a_store() => _store.ShouldEqual(ResourceNameStatus.NotReady);
    [Fact] void should_keep_the_system_store_available() => _system.ShouldEqual(ResourceNameStatus.Available);
    [Fact] void should_be_ready_once_published() => _readyAfterPublishing.ShouldBeTrue();
}
