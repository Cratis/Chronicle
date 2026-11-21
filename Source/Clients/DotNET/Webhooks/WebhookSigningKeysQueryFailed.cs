// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Exception that gets thrown when the Kernel refuses or fails the query for webhook signing public keys.
/// </summary>
/// <param name="details">The failure messages reported by the Kernel.</param>
public class WebhookSigningKeysQueryFailed(string details) : Exception($"Unable to get the webhook signing keys: {details}");
