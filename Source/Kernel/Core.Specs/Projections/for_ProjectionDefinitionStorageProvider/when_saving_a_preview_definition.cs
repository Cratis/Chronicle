// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionDefinitionStorageProvider;

public class when_saving_a_preview_definition : given.a_history_provider
{
    void Establish() => _grainId = GrainId.Create("projection", new ProjectionKey(ProjectionId.CreatePreviewId(), _eventStore).ToString());
    Task Because() => Write();
    [Fact] void should_not_record_history() => _history.DidNotReceive().Record(Arg.Any<ProjectionDefinitionHistoryEntry>());
    [Fact] void should_not_save_active() => _eventStoreStorage.Projections.DidNotReceive().Save(Arg.Any<ProjectionDefinition>());
}
