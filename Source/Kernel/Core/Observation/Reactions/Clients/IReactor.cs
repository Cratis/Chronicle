// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Defines a client observer.
/// </summary>
public interface IReactor : IGrainWithStringKey
{
    /// <summary>
    /// Start the observer.
    /// </summary>
    /// <param name="definition">The <see cref="ReactorDefinition"/> to start observing.</param>
    /// <returns>Awaitable task.</returns>
    Task SetDefinitionAndSubscribe(ReactorDefinition definition);

    /// <summary>
    /// Unsubscribe the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe();
}
