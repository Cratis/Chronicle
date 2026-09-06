// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;

/// <summary>
/// Represents where a projected value comes from, independent of any language.
/// </summary>
/// <param name="Kind">What kind of source it is.</param>
/// <param name="Value">The property path or constant the kind refers to, empty when it needs none.</param>
/// <remarks>
/// The declaration language spells a value's origin as a string - <c>$eventContext(occurred)</c>,
/// <c>$eventSourceId</c>, a property path, a literal. Every language generator needs the same reading
/// of those strings and differs only in how it writes the result, so the reading happens once and each
/// generator only decides how to spell it.
/// </remarks>
public record ProjectionValueSource(ProjectionValueKind Kind, string Value)
{
    /// <summary>
    /// Gets a source for a value that is not there.
    /// </summary>
    public static ProjectionValueSource Nothing { get; } = new(ProjectionValueKind.Nothing, string.Empty);

    /// <summary>
    /// Gets a source for the event source id.
    /// </summary>
    public static ProjectionValueSource EventSourceId { get; } = new(ProjectionValueKind.EventSourceId, string.Empty);
}
