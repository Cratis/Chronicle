// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_a_webhook_is_retired_after_gone : given.an_evaluator
{
    readonly Guid _id = Guid.NewGuid();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf() with
    {
        Disposition = AlertDisposition.Retired,
        WebhookRetirement = new(_id, _now, "https://receiver.example:8443")
    });

    [Fact] void should_raise_one_incident() => _result.Transitions.Count.ShouldEqual(1);
    [Fact] void should_use_the_retirement_identity() => _result.Transitions.OfType<AlertRaised>().Single().IncidentId.Value.ShouldEqual(_id);
    [Fact] void should_be_critical() => _result.Transitions.OfType<AlertRaised>().Single().Severity.ShouldEqual(AlertSeverity.Critical);
    [Fact] void should_be_about_the_whole_webhook() => _result.Transitions.OfType<AlertRaised>().Single().Target.Partition.ShouldEqual(AlertPartition.None);
    [Fact] void should_include_the_safe_authority() => _result.Transitions.OfType<AlertRaised>().Single().Evidence.Message.ShouldContain("https://receiver.example:8443");
    [Fact] void should_prescribe_registration() => _result.Transitions.OfType<AlertRaised>().Single().Evidence.Message.ShouldContain("Re-register");
}
