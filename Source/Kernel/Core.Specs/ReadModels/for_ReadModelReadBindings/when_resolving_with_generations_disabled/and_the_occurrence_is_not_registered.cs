// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReadBindings.when_resolving_with_generations_disabled;

public class and_the_occurrence_is_not_registered : given.a_read_binding_resolver
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _resolver.Resolve(EventStore, Namespace, _definition, (ReadModelContainerName)"x'; DROP TABLE t; --"));

    [Fact] void should_reject_the_occurrence() => _error.ShouldBeOfExactType<UnknownReadModelOccurrence>();
    [Fact] void should_not_echo_the_occurrence() => _error.Message.ShouldNotContain("DROP");
    [Fact] void should_not_resolve_a_sink() => _sinks.DidNotReceiveWithAnyArgs().GetFor(default!, default!);
}
