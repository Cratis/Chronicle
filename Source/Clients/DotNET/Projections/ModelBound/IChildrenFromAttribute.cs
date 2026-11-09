// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines an attribute that indicates that a collection property is populated from child events.
/// </summary>
public interface IChildrenFromAttribute : IKeyedAttribute
{
    /// <summary>
    /// Gets the type of event to project from.
    /// </summary>
    Type EventType { get; }

    /// <summary>
    /// Gets the optional name of the property on the parent to use as key.
    /// </summary>
    string? ParentKey { get; }

    /// <summary>
    /// Gets the optional name of the property to identify children by.
    /// </summary>
    string? IdentifiedBy { get; }

    /// <summary>
    /// Gets a value indicating whether new children are inserted first in the collection instead of appended.
    /// </summary>
    bool Prepend => false;

    /// <summary>
    /// Gets the optional name of the child property to keep the collection sorted ascending by when children are added.
    /// </summary>
    string? OrderBy => null;

    /// <summary>
    /// Gets the optional name of the child property to keep the collection sorted descending by when children are added.
    /// </summary>
    string? OrderByDescending => null;
}
