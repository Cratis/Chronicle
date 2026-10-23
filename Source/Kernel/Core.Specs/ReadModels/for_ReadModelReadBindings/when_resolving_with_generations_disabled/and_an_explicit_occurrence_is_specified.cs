// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReadBindings.when_resolving_with_generations_disabled;

public class and_an_explicit_occurrence_is_specified : given.a_read_binding_resolver
{
    async Task Because() => _binding = await _resolver.Resolve(EventStore, Namespace, _definition, "historical-container");

    [Fact] void should_resolve_the_existing_container() => _sinks.Received(1).GetFor(_definition, _definition.ContainerName);
    [Fact] void should_return_the_existing_sink() => _binding.Sink.ShouldBeSame(_sink);
    [Fact] void should_preserve_the_occurrence() => _binding.Occurrence.ShouldEqual((ReadModelContainerName)"historical-container");
}
