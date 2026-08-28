// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines a system that extracts the contextual facts pattern mining works on from an event.
/// </summary>
public interface IEventFeatureExtractor
{
    /// <summary>
    /// Extract the <see cref="EventFeatures"/> from an event.
    /// </summary>
    /// <param name="event">The <see cref="AppendedEvent"/> to extract from.</param>
    /// <returns>The extracted <see cref="EventFeatures"/>.</returns>
    EventFeatures Extract(AppendedEvent @event);
}
