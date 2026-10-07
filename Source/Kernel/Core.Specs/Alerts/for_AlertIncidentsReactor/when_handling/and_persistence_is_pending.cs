// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReactor.when_handling;

public class and_persistence_is_pending : given.an_incidents_reactor
{
    TaskCompletionSource<AlertIncidentWriteOutcome> _completion;
    bool _completedBeforePersistence;

    void Establish()
    {
        _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _incidents.Apply(Arg.Any<AlertIncidentTransition>()).Returns(_completion.Task);
    }

    async Task Because()
    {
        var handling = _reactor.Raised(_raised, _context);
        _completedBeforePersistence = handling.IsCompleted;
        _completion.SetResult(AlertIncidentWriteOutcome.Applied);
        await handling;
    }

    [Fact] void should_await_persistence_before_acknowledging() => _completedBeforePersistence.ShouldBeFalse();
}
