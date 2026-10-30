// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_the_same_projection_exists_in_two_namespaces : given.an_initializer
{
    void Establish() => _snapshot[1] = _snapshot[0] with { Namespace = new("other-tenant") };

    async Task Because() => _result = await _initializer.Initialize(_name);

    [Fact] void should_enable() => _result.ShouldBeOfExactType<GenerationInitializationEnabled>();
    [Fact] void should_reserve_each_namespace_separately() => _catalog.Records.Keys.Select(key => key.Namespace).Distinct().Count().ShouldEqual(2);
    [Fact] void should_bind_each_namespace_separately() => _catalog.Epochs.Count.ShouldEqual(2);
}
