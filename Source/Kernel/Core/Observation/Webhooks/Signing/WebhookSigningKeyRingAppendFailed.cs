// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// The exception that is thrown when an event recording a signing key change could not be appended.
/// </summary>
/// <param name="keyId">The identifier of the key the event concerns.</param>
/// <param name="eventType">The type of the event that could not be appended.</param>
/// <remarks>
/// The message names the key and the event type only, never any key material.
/// </remarks>
public class WebhookSigningKeyRingAppendFailed(WebhookSigningKeyId keyId, Type eventType)
    : Exception($"Changing webhook signing key '{keyId.Value}' failed, the '{eventType.Name}' event could not be appended");
