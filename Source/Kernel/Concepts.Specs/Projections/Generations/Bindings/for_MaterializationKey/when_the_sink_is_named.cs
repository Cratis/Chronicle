// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Concepts.Projections.Generations.Bindings.for_MaterializationKey;

public class when_the_sink_is_named : Specification
{
    MaterializationKey _default;
    MaterializationKey _reporting;
    MaterializationKey _archive;

    void Establish()
    {
        _default = new(new(Guid.Parse("fa5dc415-e617-48a0-9975-fd1580aa321a")), "tenant", "projection", "SQL", SinkConfigurationId.None);
        _reporting = _default with { SinkName = "reporting" };
        _archive = _default with { SinkName = "archive" };
    }

    [Fact] void should_differ_from_the_implicit_default() => _reporting.Value.ShouldNotEqual(_default.Value);
    [Fact] void should_differ_between_names() => _reporting.Value.ShouldNotEqual(_archive.Value);
    [Fact] void should_add_the_name_as_a_segment() => _reporting.Value.ShouldEqual($"{_default.Value}/reporting");
    [Fact] void should_match_names_without_regard_to_case() => (_default with { SinkName = "Reporting" }).Value.ShouldEqual(_reporting.Value);
    [Fact] void should_not_equal_the_key_of_another_sink() => _reporting.ShouldNotEqual(_archive);
}
