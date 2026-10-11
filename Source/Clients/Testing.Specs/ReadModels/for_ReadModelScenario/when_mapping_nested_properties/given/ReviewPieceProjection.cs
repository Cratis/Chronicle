// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

/// <summary>
/// Maps the brief's nested topic to compact review metadata.
/// </summary>
public class ReviewPieceProjection : IProjectionFor<ReviewPiece>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ReviewPiece> builder) => builder
        .NoAutoMap()
        .From<ContentBriefed>(from => from
            .Set(piece => piece.Id).ToEventSourceId()
            .Set(piece => piece.Topic).To(briefed => briefed.Brief.Topic)
            .Set(piece => piece.Author).To(briefed => briefed.Author))
        .From<DraftGenerated>(from => from.Set(piece => piece.Revision).To(e => e.Revision))
        .From<DraftEdited>(from => from.Set(piece => piece.Revision).To(e => e.Revision));
}
