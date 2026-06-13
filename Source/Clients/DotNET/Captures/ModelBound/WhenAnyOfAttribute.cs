// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.ModelBound;

/// <summary>
/// Attribute used to indicate that an event should be appended when any of the specified properties change.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="WhenAnyOfAttribute"/>.
/// </remarks>
/// <param name="properties">The properties that may change.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class WhenAnyOfAttribute(params string[] properties) : Attribute
{
    /// <summary>
    /// Gets the properties that may change.
    /// </summary>
    public string[] Properties { get; } = properties;
}
