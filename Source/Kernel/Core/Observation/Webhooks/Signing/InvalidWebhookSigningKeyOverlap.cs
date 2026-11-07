// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// The exception that is thrown when the overlap of a rotation is negative or exceeds the maximum.
/// </summary>
/// <param name="overlap">The rejected overlap.</param>
public class InvalidWebhookSigningKeyOverlap(WebhookSigningKeyOverlap overlap)
    : Exception($"A webhook signing key overlap must be between zero and {WebhookSigningKeyOverlap.Maximum.Value}, got {overlap.Value}");
