// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_a_namespace_subscription_fails_during_storage_upgrade : given.a_projections_manager_grain
{
    ProjectionDefinition _fresh;
    Exception _error;

    void Establish()
    {
        _fresh = CreateDefinition("the-projection", "the-read-model") with { Scope = ProjectionScope.Global };
        _state.Projections = [_fresh with { HasLegacyStorageFields = true, Scope = ProjectionScope.Namespaced }];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
        _observerGrain.IsSubscribed().Returns(Task.FromException<bool>(new InvalidOperationException("Subscription unavailable")));
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _grain.Register([_fresh]));
        _projectionGrain.ClearReceivedCalls();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_report_the_subscription_failure() => _error.ShouldBeOfExactType<SomeProjectionDefinitionsFailedToRegister>();
    [Fact] void should_keep_the_committed_upgrade_in_manager_state() => _state.Projections.ShouldContainOnly(_fresh);
    [Fact] void should_persist_the_committed_upgrade() => _stateStorage.Received(1).WriteStateAsync();
    [Fact] void should_retry_with_only_lossless_definitions() => _projectionGrain.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(IProjection.SetDefinition)).Select(call => (ProjectionDefinition)call.GetArguments()[0]!).ShouldEachConformTo(definition => !definition.HasLegacyStorageFields && definition.Scope == ProjectionScope.Global);
    [Fact] void should_retry_the_subscription() => _projectionGrain.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(IProjection.SetDefinition)).ShouldNotBeEmpty();
}
