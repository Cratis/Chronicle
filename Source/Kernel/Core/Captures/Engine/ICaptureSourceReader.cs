// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Defines a system that reads the current items from a capture source.
/// </summary>
public interface ICaptureSourceReader
{
    /// <summary>
    /// Gets the <see cref="SourceType"/> the reader supports.
    /// </summary>
    SourceType Type { get; }

    /// <summary>
    /// Read the current items from the source.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the capture belongs to.</param>
    /// <param name="source">The <see cref="SourceDefinition"/> describing the source.</param>
    /// <returns>The items currently at the source.</returns>
    Task<IEnumerable<JsonObject>> Read(EventStoreName eventStore, SourceDefinition source);
}
