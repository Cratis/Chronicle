// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Attribute used to categorize observers (reducers, projections, reactors).
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="CategoryAttribute"/>.
/// </remarks>
/// <param name="categories">The names of the categories.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class CategoryAttribute(params string[] categories) : Attribute
{
    /// <summary>
    /// Gets the names of the categories.
    /// </summary>
    public IEnumerable<string> Categories { get; } = categories;
}
