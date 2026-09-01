// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;

/// <summary>
/// Represents where a projected value comes from.
/// </summary>
public enum ProjectionValueKind
{
    /// <summary>
    /// A property on the event, by path.
    /// </summary>
    EventProperty = 0,

    /// <summary>
    /// The event source id of the event.
    /// </summary>
    EventSourceId = 1,

    /// <summary>
    /// A property on the event context rather than the event itself.
    /// </summary>
    EventContextProperty = 2,

    /// <summary>
    /// A constant that every language writes the same way - a number or a boolean.
    /// </summary>
    Literal = 3,

    /// <summary>
    /// A constant that needs whatever quoting the target language uses.
    /// </summary>
    Text = 4,

    /// <summary>
    /// No value - the property is cleared.
    /// </summary>
    Nothing = 5
}
