// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Storage.MongoDB.Captures;

/// <summary>
/// Represents the MongoDB representation of a capture.
/// </summary>
public class Capture
{
    /// <summary>
    /// Gets or sets the <see cref="CaptureId"/> of the capture.
    /// </summary>
    public CaptureId Id { get; set; } = CaptureId.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="CaptureName"/> of the capture.
    /// </summary>
    public CaptureName Name { get; set; } = CaptureName.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="CaptureDeclaration"/> source text.
    /// </summary>
    public CaptureDeclaration Declaration { get; set; } = CaptureDeclaration.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="CaptureStatus"/>.
    /// </summary>
    public CaptureStatus Status { get; set; }
}
