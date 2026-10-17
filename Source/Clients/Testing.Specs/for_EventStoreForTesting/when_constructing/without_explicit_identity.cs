// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_constructing;

public class without_explicit_identity : Specification
{
    EventStoreForTesting[] _stores;

    void Because()
    {
        var artifacts = new InMemoryClientArtifactsProvider();
        _stores =
        [
            new(),
            new(null, artifacts),
            new(null, artifacts, null),
            new(null, artifacts, null, new ScenarioTime()),
            new(null, artifacts, null, new ScenarioTime(), new KernelCapabilitiesForTesting())
        ];
    }

    [Fact] void should_keep_the_default_name() => _stores.Select(store => store.Name.Value).ShouldEachConformTo(name => name == "testing");
    [Fact] void should_keep_the_default_namespace() => _stores.Select(store => store.Namespace.Value).ShouldEachConformTo(ns => ns == "default");

    void Destroy()
    {
        foreach (var store in _stores) store.Dispose();
    }
}
