// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Alerts.for_AlertIncidentFold.given;

public class a_recorded_transition : Specification
{
    protected AlertIncidentTransition _raise;
    protected AlertIncident? _current;
    protected AlertIncidentTransition _transition;
    protected AlertIncidentFoldResult _result;

    void Establish()
    {
        var occurred = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2));
        _raise = new(AlertIncidentTransitionKind.Raised, new IncidentId(Guid.NewGuid()), new("store", "tenant", "observer", EventSequenceId.Log, "[none]"), "unknown-condition", AlertSeverity.Warning, new(3, occurred, occurred, FailureKind.Handling, "evidence"), null, occurred, 10UL);
        _transition = _raise;
    }
}
