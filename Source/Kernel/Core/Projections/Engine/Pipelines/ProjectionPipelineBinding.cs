// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>Associates a live pipeline with the compiled graph it actually owns.</summary>
/// <param name="Projection">The pipeline's compiled graph.</param>
/// <param name="Pipeline">The pipeline which owns buffered work for that graph.</param>
public record ProjectionPipelineBinding(IProjection Projection, IProjectionPipeline Pipeline)
{
    /// <summary>Gets the definition fingerprint captured when the pipeline was created.</summary>
    public string Fingerprint { get; } = Projection.DefinitionFingerprint;
}
