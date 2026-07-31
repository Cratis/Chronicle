// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents a capture as it is persisted - the declaration source and its lifecycle status.
/// The compiled <see cref="CaptureDefinition"/> is derived from the declaration on demand and is never persisted.
/// </summary>
/// <param name="Id">The unique <see cref="CaptureId"/>.</param>
/// <param name="Name">The <see cref="CaptureName"/>.</param>
/// <param name="Declaration">The <see cref="CaptureDeclaration"/> source text.</param>
/// <param name="Status">The <see cref="CaptureStatus"/>.</param>
public record Capture(
    CaptureId Id,
    CaptureName Name,
    CaptureDeclaration Declaration,
    CaptureStatus Status);
