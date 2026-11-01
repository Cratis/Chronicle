// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionDefinitionStorageProvider;

public class when_the_active_save_is_retried : given.a_history_provider
{
    void Establish() => _eventStoreStorage.Projections.Save(_firstDefinition).Returns(_ => throw new InvalidOperationException("save failed"), _ => Task.CompletedTask);
    async Task Because()
    {
        await Catch.Exception(Write);
        await Write();
    }
    [Fact] Task should_retry_the_same_history_identity() => _history.Received(2).Record(Arg.Is<ProjectionDefinitionHistoryEntry>(_ => _.Digest == ProjectionFactory.DefinitionDigestFor(_firstDefinition)));
    [Fact] Task should_retry_the_active_save() => _eventStoreStorage.Projections.Received(2).Save(_firstDefinition);
}
