// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// The exception that is thrown when a webhook identifier contains a signing delimiter.
/// </summary>
public sealed class InvalidWebhookMessageId() : Exception("A webhook message identifier must not contain '.'.");
