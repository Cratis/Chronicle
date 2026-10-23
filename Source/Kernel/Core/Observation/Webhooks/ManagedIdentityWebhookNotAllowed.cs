// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception that is thrown when a webhook requests an identity, scope or receiver the operator has not approved.
/// </summary>
public class ManagedIdentityWebhookNotAllowed() : Exception("Managed identity webhook is not approved by the kernel configuration.");
