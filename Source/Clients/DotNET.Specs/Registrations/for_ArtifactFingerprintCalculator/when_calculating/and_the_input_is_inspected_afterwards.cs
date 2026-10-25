// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ArtifactFingerprintCalculator.when_calculating;

public class and_the_input_is_inspected_afterwards : given.a_complete_set_of_definitions
{
    ArtifactDefinitionPayloads _definitions;
    object _lastUpdated;
    string[] _fromOrder;
    string[] _propertyOrder;

    void Establish()
    {
        _definitions = Reordered();
        var projection = _definitions.Projections.First();
        _lastUpdated = projection.LastUpdated;
        _fromOrder = projection.From.Keys.Select(_ => _.Id).ToArray();
        _propertyOrder = projection.From.First().Value.Properties.Keys.ToArray();
    }

    void Because() => ArtifactFingerprintCalculator.Calculate(_definitions);

    [Fact]
    void should_preserve_last_updated() => _definitions.Projections.First().LastUpdated.ShouldEqual(_lastUpdated);

    [Fact]
    void should_preserve_the_event_store() => _definitions.EventTypes.First().EventStore.ShouldEqual("first-store");

    [Fact]
    void should_preserve_event_dictionary_order() => _definitions.Projections.First().From.Keys.Select(_ => _.Id).SequenceEqual(_fromOrder).ShouldBeTrue();

    [Fact]
    void should_preserve_property_dictionary_order() => _definitions.Projections.First().From.First().Value.Properties.Keys.SequenceEqual(_propertyOrder).ShouldBeTrue();
}
