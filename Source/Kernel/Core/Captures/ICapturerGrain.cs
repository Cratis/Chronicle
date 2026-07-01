// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the grain responsible for executing a capture.
/// </summary>
public interface ICapturerGrain : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Sets the definition for the capturer.
    /// </summary>
    /// <param name="definition">The capture definition.</param>
    /// <returns>Awaitable task.</returns>
    Task SetDefinition(CaptureDefinition definition);

    /// <summary>
    /// Executes one capture cycle.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Capture();
}
