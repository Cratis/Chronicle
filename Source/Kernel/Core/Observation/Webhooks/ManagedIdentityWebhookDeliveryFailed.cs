// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception that is thrown when a managed identity delivery fails, without retaining token-bearing transport details.
/// </summary>
public class ManagedIdentityWebhookDeliveryFailed() : Exception("Managed identity webhook delivery failed.");
