// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_removing_a_joined_child;

public class JoinedProjectSummaryProjection : IProjectionFor<JoinedProjectSummary>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<JoinedProjectSummary> builder) => builder
        .From<JoinedProjectRegistered>()
        .Children(project => project.Notes, children => children
            .IdentifiedBy(note => note.NoteId)
            .From<JoinedProjectNoted>(from => from.UsingKey(e => e.NoteId).UsingParentKey(e => e.ProjectId))
            .RemovedWithJoin<JoinedProjectNoteRemovedViaJoin>(removed => removed.UsingKey(e => e.NoteId)));
}
