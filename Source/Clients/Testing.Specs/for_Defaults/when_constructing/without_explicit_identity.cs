// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_Defaults.when_constructing;

public class without_explicit_identity : Specification
{
    Defaults[] _defaults;

    void Because()
    {
        var artifacts = new InMemoryClientArtifactsProvider();
        _defaults =
        [
            new(),
            new(artifacts),
            new(artifacts, null, null),
            new(artifacts, null, null, new ScenarioTime())
        ];
    }

    [Fact] void should_keep_the_default_name() => _defaults.Select(defaults => defaults.EventStore.Name.Value).ShouldEachConformTo(name => name == "testing");
    [Fact] void should_keep_the_default_namespace() => _defaults.Select(defaults => defaults.EventStore.Namespace.Value).ShouldEachConformTo(ns => ns == "default");

    void Destroy()
    {
        foreach (var defaults in _defaults) ((IDisposable)defaults.EventStore).Dispose();
    }
}
