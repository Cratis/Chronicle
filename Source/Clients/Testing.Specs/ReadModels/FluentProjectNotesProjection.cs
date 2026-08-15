// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Fluent projection exercising the formal <c>.Clear(...)</c> everywhere <c>.Set(...)</c> reaches - at the root,
/// on a child item, and on a member of a nested object - alongside the older <c>.Set(...).ToValue(null)</c>
/// spelling of the same clear.
/// </summary>
public class FluentProjectNotesProjection : IProjectionFor<FluentProjectNotes>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<FluentProjectNotes> builder) => builder
        .From<ProjectNoted>(_ => _
            .Set(m => m.Note).To(e => e.Note))
        .From<ProjectNoteCleared>(_ => _
            .Clear(m => m.Note))
        .Nested(m => m.Summary, summary => summary
            .From<FluentProjectSummarised>(_ => _
                .Set(m => m.Headline).To(e => e.Headline)
                .Set(m => m.Note).To(e => e.Note))
            .From<FluentProjectSummaryNoteCleared>(_ => _
                .Clear(m => m.Note)))
        .Children(m => m.Tasks, tasks => tasks
            .IdentifiedBy(_ => _.Id)
            .From<FluentProjectTaskAdded>(_ => _
                .UsingKey(e => e.TaskId)
                .Set(m => m.Title).To(e => e.Title)
                .Set(m => m.Note).To(e => e.Note))
            .From<FluentProjectTaskNoteCleared>(_ => _
                .UsingKey(e => e.TaskId)
                .Clear(m => m.Note)));
}
