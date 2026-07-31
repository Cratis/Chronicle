// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Holds the well known causation for events ingested by a capture.
/// </summary>
public static class CaptureCausation
{
    /// <summary>
    /// The causation property holding the identifier of the capture.
    /// </summary>
    public const string CaptureId = "captureId";

    /// <summary>
    /// The causation property holding the name of the capture.
    /// </summary>
    public const string CaptureName = "captureName";

    /// <summary>
    /// The <see cref="CausationType"/> for events ingested by a capture.
    /// </summary>
    public static readonly CausationType Type = new("capture");

    /// <summary>
    /// Create the <see cref="Causation"/> for events ingested by a capture.
    /// </summary>
    /// <param name="capture">The <see cref="Capture"/> that ingested the events.</param>
    /// <returns>The <see cref="Causation"/>.</returns>
    public static Causation For(Capture capture) => new(
        DateTimeOffset.UtcNow,
        Type,
        new Dictionary<string, string>
        {
            [CaptureId] = capture.Id.ToString(),
            [CaptureName] = capture.Name
        });
}
