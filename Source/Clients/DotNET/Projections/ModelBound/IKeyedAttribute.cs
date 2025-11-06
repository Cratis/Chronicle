// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines an attribute that has a key property.
/// </summary>
public interface IKeyedAttribute
{
    /// <summary>
    /// Gets the name of the property to use as key.
    /// </summary>
    string? Key { get; }
}
