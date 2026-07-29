// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.Storage.ExternalServices;

/// <summary>
/// Defines a system for working with <see cref="ExternalServiceDefinition"/>.
/// </summary>
public interface IExternalServiceDefinitionsStorage
{
    /// <summary>
    /// Get all <see cref="ExternalServiceDefinition">definitions</see> registered.
    /// </summary>
    /// <returns>A collection of <see cref="ExternalServiceDefinition"/>.</returns>
    Task<IEnumerable<ExternalServiceDefinition>> GetAll();

    /// <summary>
    /// Observe all <see cref="ExternalServiceDefinition">definitions</see> registered.
    /// </summary>
    /// <returns>A <see cref="ISubject{T}"/> of <see cref="IEnumerable{T}"/> of <see cref="ExternalServiceDefinition"/>.</returns>
    ISubject<IEnumerable<ExternalServiceDefinition>> ObserveAll();

    /// <summary>
    /// Check if a <see cref="ExternalServiceDefinition"/> exists by its <see cref="ExternalServiceId"/>.
    /// </summary>
    /// <param name="id"><see cref="ExternalServiceId"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    Task<bool> Has(ExternalServiceId id);

    /// <summary>
    /// Get a specific <see cref="ExternalServiceDefinition"/> by its <see cref="ExternalServiceId"/>.
    /// </summary>
    /// <param name="id"><see cref="ExternalServiceId"/> to get for.</param>
    /// <returns><see cref="ExternalServiceDefinition"/>.</returns>
    Task<ExternalServiceDefinition> Get(ExternalServiceId id);

    /// <summary>
    /// Delete a <see cref="ExternalServiceDefinition"/> by its <see cref="ExternalServiceId"/>.
    /// </summary>
    /// <param name="id"><see cref="ExternalServiceId"/> of the <see cref="ExternalServiceDefinition"/> to delete.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(ExternalServiceId id);

    /// <summary>
    /// Save a <see cref="ExternalServiceDefinition"/>.
    /// </summary>
    /// <param name="definition">Definition to save.</param>
    /// <returns>Async task.</returns>
    Task Save(ExternalServiceDefinition definition);
}
