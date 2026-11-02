// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionDefinitionStorageProvider;

public class when_active_definition_save_fails : given.a_history_provider
{
    Exception _exception;
    void Establish() => _eventStoreStorage.Projections.Save(_firstDefinition).Returns(Task.FromException(new InvalidOperationException("save failed")));
    async Task Because() => _exception = await Catch.Exception(Write);
    [Fact] void should_propagate_the_failure() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] Task should_have_recorded_history() => _history.Received(1).Record(Arg.Any<ProjectionDefinitionHistoryEntry>());
}
