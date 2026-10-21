// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_acquiring_token;

internal static class RequestOptionExtensions
{
    internal static EventStoreName? GetEventStore(this HttpRequestOptions options)
    {
        options.TryGetValue(WebhookRequestOptions.EventStore, out var store);
        return store;
    }
}
