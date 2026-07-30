// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Captures;

/// <summary>
/// Provides extension methods for converting between Kernel and SQL capture representations.
/// </summary>
public static class CaptureConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Captures.Capture"/> to a SQL <see cref="Capture"/>.
    /// </summary>
    /// <param name="capture">The Kernel capture.</param>
    /// <returns>The SQL capture.</returns>
    public static Capture ToSql(this Concepts.Captures.Capture capture) =>
        new()
        {
            Id = capture.Id.Value.ToString(),
            Name = capture.Name,
            Declaration = capture.Declaration,
            Status = capture.Status
        };

    /// <summary>
    /// Converts a SQL <see cref="Capture"/> to a Kernel <see cref="Concepts.Captures.Capture"/>.
    /// </summary>
    /// <param name="capture">The SQL capture.</param>
    /// <returns>The Kernel capture.</returns>
    public static Concepts.Captures.Capture ToKernel(this Capture capture) =>
        new(
            new CaptureId(Guid.Parse(capture.Id)),
            capture.Name,
            capture.Declaration,
            capture.Status);
}
