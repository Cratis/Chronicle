// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents a system that can provide the <see cref="Observers"/> configuration for an observer.
/// </summary>
public interface IConfigurationForObserverProvider
{
    /// <summary>
    /// Gets the <see cref="Observers"/> configuration for the.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> that uniquely identifies the observer.</param>
    /// <returns>The configuration.</returns>
    Task<Observers> GetFor(ObserverKey observerKey);
}
