// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Storage.Captures;

/// <summary>
/// Defines a system for working with <see cref="Capture">captures</see>.
/// </summary>
public interface ICapturesStorage
{
    /// <summary>
    /// Get all <see cref="Capture">captures</see> registered.
    /// </summary>
    /// <returns>A collection of <see cref="Capture"/>.</returns>
    Task<IEnumerable<Capture>> GetAll();

    /// <summary>
    /// Observe all <see cref="Capture">captures</see> registered.
    /// </summary>
    /// <returns>A <see cref="ISubject{T}"/> of <see cref="IEnumerable{T}"/> of <see cref="Capture"/>.</returns>
    ISubject<IEnumerable<Capture>> ObserveAll();

    /// <summary>
    /// Check if a <see cref="Capture"/> exists by its <see cref="CaptureId"/>.
    /// </summary>
    /// <param name="id"><see cref="CaptureId"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    Task<bool> Has(CaptureId id);

    /// <summary>
    /// Get a specific <see cref="Capture"/> by its <see cref="CaptureId"/>.
    /// </summary>
    /// <param name="id"><see cref="CaptureId"/> to get for.</param>
    /// <returns><see cref="Capture"/>.</returns>
    Task<Capture> Get(CaptureId id);

    /// <summary>
    /// Delete a <see cref="Capture"/> by its <see cref="CaptureId"/>.
    /// </summary>
    /// <param name="id"><see cref="CaptureId"/> of the <see cref="Capture"/> to delete.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(CaptureId id);

    /// <summary>
    /// Save a <see cref="Capture"/>.
    /// </summary>
    /// <param name="capture">Capture to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(Capture capture);
}
