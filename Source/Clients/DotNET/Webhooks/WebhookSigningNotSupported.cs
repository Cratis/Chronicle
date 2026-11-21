// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// The exception that is thrown when a webhook definition builder does not support signing.
/// </summary>
public class WebhookSigningNotSupported() : Exception("This webhook definition builder does not support signing.");
