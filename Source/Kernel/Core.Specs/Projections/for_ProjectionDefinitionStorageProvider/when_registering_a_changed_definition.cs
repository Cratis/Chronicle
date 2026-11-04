// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionDefinitionStorageProvider;

public class when_registering_a_changed_definition : given.a_history_provider
{
    async Task Because()
    {
        await Write();
        await Write();
        _state.State.Returns(_firstDefinition with { AutoMap = AutoMap.Disabled });
        await Write();
        await Write();
    }

    [Fact] Task should_record_once_per_new_digest() => _history.Received(2).Record(Arg.Any<ProjectionDefinitionHistoryEntry>());
}
