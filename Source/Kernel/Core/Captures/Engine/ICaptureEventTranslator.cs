// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Defines a system that translates a single incoming event into the private events a capture appends for it.
/// </summary>
public interface ICaptureEventTranslator
{
    /// <summary>
    /// Translate an incoming event.
    /// </summary>
    /// <param name="definition">The <see cref="CaptureDefinition"/> of the capture.</param>
    /// <param name="previous">The previously captured state for the key the event resolves to, if any.</param>
    /// <param name="content">The content of the incoming event, which is the captured item.</param>
    /// <param name="context">The event context of the incoming event as JSON, see <see cref="CapturedEventContext"/>.</param>
    /// <returns>The <see cref="CaptureEventTranslation"/>.</returns>
    /// <exception cref="MissingKeyForCapturedEvent">The event does not resolve a key.</exception>
    CaptureEventTranslation Translate(CaptureDefinition definition, JsonObject? previous, JsonObject content, JsonObject context);
}
