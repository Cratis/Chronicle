// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.ModelBound;

/// <summary>
/// Attribute used to indicate that an event should be appended when all of the specified properties change.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="WhenAllOfAttribute"/>.
/// </remarks>
/// <param name="properties">The properties that must change.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class WhenAllOfAttribute(params string[] properties) : Attribute
{
    /// <summary>
    /// Gets the properties that must change.
    /// </summary>
    public string[] Properties { get; } = properties;
}
