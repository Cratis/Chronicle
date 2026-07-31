// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Defines a system that maps an observed <see cref="CaptureChange"/> into event content for an <see cref="AppendDefinition"/>.
/// </summary>
public interface ICaptureContentMapper
{
    /// <summary>
    /// Map the item behind a <see cref="CaptureChange"/> into event content using the field assignments of an <see cref="AppendDefinition"/>.
    /// When the append definition has no field assignments, the entire item is used as content.
    /// </summary>
    /// <param name="append">The <see cref="AppendDefinition"/> holding the field assignments.</param>
    /// <param name="change">The <see cref="CaptureChange"/> holding the item.</param>
    /// <returns>The mapped event content.</returns>
    JsonObject Map(AppendDefinition append, CaptureChange change);
}
