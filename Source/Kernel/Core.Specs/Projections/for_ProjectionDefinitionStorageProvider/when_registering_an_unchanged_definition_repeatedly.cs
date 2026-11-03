// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionDefinitionStorageProvider;

public class when_registering_an_unchanged_definition_repeatedly : given.a_history_provider
{
    async Task Because()
    {
        await Write();
        await Write();
        await Write();
    }

    [Fact] Task should_record_history_exactly_once() => _history.Received(1).Record(Arg.Any<ProjectionDefinitionHistoryEntry>());
    [Fact] Task should_still_save_the_active_definition_each_time() => _eventStoreStorage.Projections.Received(3).Save(_firstDefinition);
}
