// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService;

public class when_called_in_process : given.a_compliance_boundary
{
    string[] _notCovered;

    void Establish() => _context = default;

    async Task Because()
    {
        _notCovered = await Operate(allowNew: false);
        await _service.Release(ReleaseRequest(strict: false), _context);
    }

    [Fact] void should_cover_the_eligible_stores() => _notCovered.ShouldBeEmpty();
    [Fact] void should_not_look_up_a_remote_caller() => _decisions.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_erase_the_requested_store() => _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_log_each_operation() => _audit.Entries.ShouldEqual(2);
}
