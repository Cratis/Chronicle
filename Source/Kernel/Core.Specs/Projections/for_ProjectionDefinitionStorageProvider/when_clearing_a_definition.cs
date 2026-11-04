// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionDefinitionStorageProvider;

public class when_clearing_a_definition : given.a_history_provider
{
    Task Because() => _provider.ClearStateAsync("projection", _grainId, _state);
    [Fact] void should_delete_the_active_definition() => _eventStoreStorage.Projections.Received(1).Delete(_firstDefinition.Identifier);
    [Fact] void should_not_touch_history() => _history.ReceivedCalls().ShouldBeEmpty();
}
