// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_notifying_changes.and_read_model_has_protected_values;

/// <summary>
/// A protected read model whose handled event left no protected state - for instance a deferred event, which the
/// protection step leaves alone. The plaintext current state is never published in its place.
/// </summary>
public class and_the_pipeline_did_not_protect_the_state : given.a_change_for_a_model_with_protected_values
{
    protected override ProjectionEventContext WhatThePipelineReturns(ProjectionEventContext protectedContext) =>
        new(protectedContext.Key, protectedContext.Event, protectedContext.Changeset, protectedContext.OperationType, false);

    async Task Because() => await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_not_notify_the_plaintext_state() => _notifier.DidNotReceive().Notify(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
