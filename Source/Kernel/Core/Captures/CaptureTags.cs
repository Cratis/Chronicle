// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Holds the well known <see cref="Tag">tags</see> stamped on events ingested by a capture,
/// making captured events queryable by capture.
/// </summary>
public static class CaptureTags
{
    /// <summary>
    /// The tag every captured event carries.
    /// </summary>
    public static readonly Tag Capture = new("Capture");

    /// <summary>
    /// Get the tags for events ingested by a capture - the <see cref="Capture"/> tag and the capture's name.
    /// </summary>
    /// <param name="name">The <see cref="CaptureName"/> of the capture.</param>
    /// <returns>The tags.</returns>
    public static IEnumerable<Tag> For(CaptureName name) => [Capture, new Tag(name)];
}
