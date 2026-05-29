// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines a grain that supervises trigger scheduling for a capture.
/// </summary>
public interface ICaptureTrigger : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Configures the trigger with a capture definition.
    /// </summary>
    /// <param name="definition">The capture definition.</param>
    /// <returns>Awaitable task.</returns>
    Task Configure(CaptureDefinition definition);

    /// <summary>
    /// Invokes a single trigger for the configured capture.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Trigger();
}
