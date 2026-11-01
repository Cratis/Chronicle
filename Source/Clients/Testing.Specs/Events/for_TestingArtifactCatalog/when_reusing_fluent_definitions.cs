// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Testing.Events.for_TestingArtifactCatalog;

public class when_reusing_fluent_definitions : Specification
{
    CachedFluentProjectionDefinitionsSource _source;
    ProjectionDefinition _second;
    int _builds;

    void Because()
    {
        _source = new();
        var first = _source.Get(default, Discover).Definitions[typeof(Model)];
        first.Identifier = "changed";
        _second = _source.Get(default, Discover).Definitions[typeof(Model)];
    }

    [Fact] void should_build_once() => _builds.ShouldEqual(1);
    [Fact] void should_return_an_independent_wire_definition() => _second.Identifier.ShouldEqual("original");

    (Dictionary<Type, ProjectionDefinition> Definitions, Dictionary<Type, Exception> Failures, Dictionary<Type, string> ContainerNames) Discover()
    {
        _builds++;
        return (new() { [typeof(Model)] = new() { Identifier = "original", ReadModel = "model" } }, [], new() { [typeof(Model)] = "container" });
    }

    record Model;
}
