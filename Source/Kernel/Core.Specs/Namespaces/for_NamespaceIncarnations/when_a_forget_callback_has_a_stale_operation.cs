// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnations;

public class when_a_forget_callback_has_a_stale_operation : given.incarnation_routing
{
    Exception _error;

    void Establish() => _state = NamespaceLifecycle.Open(_namespace, NamespaceIncarnationId.New(), DateTimeOffset.UtcNow)
        .BeginRemoval(Guid.NewGuid(), DateTimeOffset.UtcNow).BeginAttempt(DateTimeOffset.UtcNow).CompleteStage(DateTimeOffset.UtcNow).IssueStage();

    async Task Because() => _error = await Catch.Exception(() => _routing.ValidateForget(_eventStore, _namespace, _state.Incarnation, Guid.NewGuid()));

    [Fact] void should_reject_a_callback_from_another_removal() => _error.ShouldBeOfExactType<NamespaceLifecycleChanged>();
}
