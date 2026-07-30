// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Defines a system that detects changes between two observations of a capture's source.
/// </summary>
public interface ICaptureChangeDetector
{
    /// <summary>
    /// Detect the changes between the previously observed items and the currently observed items.
    /// </summary>
    /// <param name="previous">The previously observed items, keyed by the capture's key property.</param>
    /// <param name="current">The currently observed items, keyed by the capture's key property.</param>
    /// <returns>The <see cref="CaptureChange">changes</see> between the two observations.</returns>
    IEnumerable<CaptureChange> Detect(IReadOnlyDictionary<string, JsonObject> previous, IReadOnlyDictionary<string, JsonObject> current);
}
