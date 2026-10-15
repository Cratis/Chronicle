// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads;

public class when_describing_a_pinned_projection : given.a_decision_reader
{
    DecisionReadDescription _description;

    void Establish() => _definition.GenerationDelivery = EventGenerationDelivery.Pinned;
    void Because() => _description = _reader.Describe<Model>();

    [Fact] void should_report_the_projection_shape() => _description.Admission.IsAdmitted.ShouldBeTrue();
    [Fact] void should_report_the_delivery_requirement() => _description.UsesPinnedGenerationDelivery.ShouldBeTrue();
    [Fact] void should_describe_the_watched_types() => _description.EventTypes.Single().Id.Value.ShouldEqual("created");
    [Fact] void should_not_perform_a_fold() => _folds.ShouldEqual(0);
    [Fact] void should_not_contact_the_kernel() => _services.ReceivedCalls().ShouldBeEmpty();
}
