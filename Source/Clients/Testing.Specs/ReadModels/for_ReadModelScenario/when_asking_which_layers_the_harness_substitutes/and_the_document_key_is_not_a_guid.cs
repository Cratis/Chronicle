// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asking_which_layers_the_harness_substitutes;

/// <summary>
/// Seeds a string-concept-keyed read model end to end and asks the same scenario both questions: the read
/// model materializes exactly as it should, and the scenario still reports that the key's stored form is
/// modeled here rather than round-tripped. A green projection and an uncovered layer are not alternatives.
/// </summary>
public class and_the_document_key_is_not_a_guid : Specification
{
    ReadModelScenario<SupplierProfile> _scenario;
    EventSourceId _orgNumber;
    IReadOnlyList<ReadModelSubstitution> _substitutions;

    void Establish()
    {
        _scenario = new ReadModelScenario<SupplierProfile>();
        _orgNumber = new EventSourceId("912345678");
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_orgNumber).Events(new SupplierOnboarded("Acme Supplies"));
        _substitutions = _scenario.Substitutions;
    }

    [Fact] void should_still_project_the_read_model() => _scenario.Instance!.Name.ShouldEqual("Acme Supplies");
    [Fact] void should_still_key_it_by_the_org_number() => _scenario.Instance!.Id.Value.ShouldEqual("912345678");
    [Fact] void should_report_the_sink() => _substitutions.Single().Layer.ShouldEqual(ReadModelSubstitutedLayer.Sink);
    [Fact] void should_name_the_key_property() => _substitutions.Single().Shape.ShouldContain(nameof(SupplierProfile.Id));
    [Fact] void should_name_the_underlying_key_type() => _substitutions.Single().Shape.ShouldContain(nameof(String));
}
