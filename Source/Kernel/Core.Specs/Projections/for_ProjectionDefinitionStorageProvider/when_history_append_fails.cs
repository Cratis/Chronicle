// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionDefinitionStorageProvider;

public class when_history_append_fails : given.a_history_provider
{
    Exception _exception;
    void Establish() => _history.Record(Arg.Any<ProjectionDefinitionHistoryEntry>()).Returns(Task.FromException(new InvalidOperationException("history failed")));
    async Task Because() => _exception = await Catch.Exception(Write);
    [Fact] void should_not_report_a_successful_registration() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] Task should_not_save_the_active_definition() => _eventStoreStorage.Projections.DidNotReceive().Save(_firstDefinition);
}
