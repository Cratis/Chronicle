// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines an attribute that is bound to a specific event type.
/// </summary>
public interface IEventBoundAttribute
{
    /// <summary>
    /// Gets the type of event this attribute is bound to.
    /// </summary>
    Type EventType { get; }
}

