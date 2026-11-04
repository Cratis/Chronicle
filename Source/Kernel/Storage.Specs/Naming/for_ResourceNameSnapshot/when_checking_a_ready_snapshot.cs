// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameSnapshot;

public class when_checking_a_ready_snapshot : Specification
{
    ResourceNameSnapshot _snapshot;
    ResourceNameStatus _status;

    void Establish() => _snapshot = new();

    void Because() => _status = _snapshot.Check("orders", "Default");

    [Fact] void should_be_ready() => _snapshot.IsReady.ShouldBeTrue();
    [Fact] void should_have_no_quarantine() => _snapshot.HasQuarantine.ShouldBeFalse();
    [Fact] void should_be_available() => _status.ShouldEqual(ResourceNameStatus.Available);
}
