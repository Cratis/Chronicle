// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_a_gone_webhook_is_still_retired : given.an_evaluator
{
    readonly Guid _id = Guid.NewGuid();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf() with { Disposition = AlertDisposition.Retired, WebhookRetirement = new(_id, _now, "https://receiver.example") }, new OpenIncident(new(_id), AlertConditionKind.WebhookEndpointGone, AlertSeverity.Critical, AlertPartition.None));

    [Fact] void should_neither_duplicate_nor_clear_the_incident() => _result.Transitions.ShouldBeEmpty();
}
