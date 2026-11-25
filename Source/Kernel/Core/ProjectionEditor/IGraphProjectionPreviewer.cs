// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Defines a service that previews a graph projection in memory.
/// </summary>
public interface IGraphProjectionPreviewer
{
    /// <summary>
    /// Preview a graph projection.
    /// </summary>
    /// <param name="request">The <see cref="PreviewGraphProjection"/> request.</param>
    /// <returns>The <see cref="GraphProjectionPreview"/>; compile errors, view errors and event errors are part of it, not exceptions.</returns>
    Task<GraphProjectionPreview> Preview(PreviewGraphProjection request);
}
