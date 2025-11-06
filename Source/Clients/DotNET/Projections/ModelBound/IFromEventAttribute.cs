// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines an attribute that indicates that a read model or property is populated from an event.
/// </summary>
public interface IFromEventAttribute
{
    /// <summary>
    /// Gets the type of event this attribute projects from.
    /// </summary>
    Type EventType { get; }
}
