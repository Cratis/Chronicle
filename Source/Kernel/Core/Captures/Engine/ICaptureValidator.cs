// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Defines a system that validates a compiled <see cref="CaptureDefinition"/> against the event store it will run in -
/// referenced external services, event types, scheduling and the capabilities of the capturing engine.
/// </summary>
public interface ICaptureValidator
{
    /// <summary>
    /// Validate a <see cref="CaptureDefinition"/> for an event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the capture belongs to.</param>
    /// <param name="definition">The <see cref="CaptureDefinition"/> to validate.</param>
    /// <returns>The <see cref="CaptureValidationMessage">messages</see> - empty when the definition is valid.</returns>
    Task<IEnumerable<CaptureValidationMessage>> Validate(EventStoreName eventStore, CaptureDefinition definition);
}
