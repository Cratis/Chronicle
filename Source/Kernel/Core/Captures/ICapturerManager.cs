// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines a system responsible for supervising capturers and their triggers for an event store.
/// </summary>
public interface ICapturerManager : IGrainWithStringKey
{
    /// <summary>
    /// Ensure the existence of the capturer manager.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Ensure();

    /// <summary>
    /// Registers capture definitions and sets up corresponding capturer and trigger grains.
    /// </summary>
    /// <param name="definitions">The capture definitions to register.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(IEnumerable<CaptureDefinition> definitions);
}
