// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

/// <summary>
/// Maps two event types onto the same nested metadata property.
/// </summary>
public class NestedPieceProjection : IProjectionFor<NestedPiece>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<NestedPiece> builder) => builder
        .NoAutoMap()
        .From<ContentBriefed>(from => from.Set(piece => piece.Meta.Topic).To(e => e.Brief.Topic))
        .From<ContentRebriefed>(from => from.Set(piece => piece.Meta.Topic).To(e => e.Brief.Topic))
        .From<DraftGenerated>(from => from.Set(piece => piece.Revision).To(e => e.Revision))
        .From<DraftEdited>(from => from.Set(piece => piece.Revision).To(e => e.Revision));
}
