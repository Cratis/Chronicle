// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Webhooks;

/// <summary>
/// Represents the request for testing a webhook endpoint.
/// </summary>
[ProtoContract]
public class TestWebhookRequest
{
    /// <summary>
    /// Gets or sets the webhook target to test.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public WebhookTarget Target { get; set; } = new();
}
