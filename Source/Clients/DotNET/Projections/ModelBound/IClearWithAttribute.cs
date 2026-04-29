// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines an attribute that indicates what event clears (nulls) a nested single-object property.
/// </summary>
public interface IClearWithAttribute : IProjectionAnnotation
{
    /// <summary>
    /// Gets the type of event that clears the nested object.
    /// </summary>
    Type EventType { get; }
}
