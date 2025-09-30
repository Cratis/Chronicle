// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Observation.Webhooks;

/// <summary>
/// Represents a target for a webhook.
/// </summary>
/// <param name="Url">The url.</param>
public record WebhookTarget(string Url);