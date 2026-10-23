// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// Request-local context for webhook and OAuth target validation.
/// </summary>
public static class WebhookRequestOptions
{
    /// <summary>
    /// The event store whose host allow-list applies to this request.
    /// </summary>
    public static readonly HttpRequestOptionsKey<EventStoreName> EventStore = new("Chronicle.Webhooks.EventStore");
}
