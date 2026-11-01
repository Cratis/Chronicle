// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReadBindings.when_resolving_with_generations_disabled;

public class and_the_projection_is_child_only : given.a_read_binding_resolver
{
    void Establish() => _visibilities.GetFor(EventStore, Namespace, _definition).Returns(ReadModelInstanceVisibility.IncludeUninitialized);

    async Task Because() => _binding = await _resolver.Resolve(EventStore, Namespace, _definition);

    [Fact] void should_include_uninitialized_roots() => _binding.Visibility.ShouldEqual(ReadModelInstanceVisibility.IncludeUninitialized);
    [Fact] void should_return_the_existing_sink() => _binding.Sink.ShouldBeSame(_sink);
}
