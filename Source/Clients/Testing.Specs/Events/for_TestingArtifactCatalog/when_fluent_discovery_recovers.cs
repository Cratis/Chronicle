// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Testing.Events.for_TestingArtifactCatalog;

public class when_fluent_discovery_recovers : Specification
{
    CachedFluentProjectionDefinitionsSource _source;
    int _builds;
    int _failures;

    void Because()
    {
        _source = new();
        _source.Get(default, Discover);
        _failures = _source.Get(default, Discover).Failures.Count;
        _source.Get(default, Discover);
    }

    [Fact] void should_retry_a_failed_discovery() => _builds.ShouldEqual(2);
    [Fact] void should_not_retain_an_old_failure() => _failures.ShouldEqual(0);

    (Dictionary<Type, ProjectionDefinition> Definitions, Dictionary<Type, Exception> Failures, Dictionary<Type, string> ContainerNames) Discover()
    {
        _builds++;
        return _builds == 1 ? ([], new() { [typeof(Model)] = new InvalidOperationException("temporarily unavailable") }, []) : ([], [], []);
    }

    record Model;
}
