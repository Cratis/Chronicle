// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Fluent projection pinning that <c>.Set(...).ToValue(null)</c> still clears - it used to write the literal string
/// "null" - alongside a <c>ToValue</c> carrying a real constant, which must be untouched by the clear handling.
/// </summary>
public class FluentArchivedNoteProjection : IProjectionFor<FluentArchivedNote>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<FluentArchivedNote> builder) => builder
        .From<ProjectNoted>(_ => _
            .Set(m => m.Note).To(e => e.Note)
            .Set(m => m.Status).ToValue("open"))
        .From<ProjectNoteCleared>(_ => _
            .Set(m => m.Note).ToValue(null)
            .Set(m => m.Status).ToValue("archived"));
}
