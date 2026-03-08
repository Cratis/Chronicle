// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Webhooks;

/// <summary>
/// Represents the response for testing a webhook endpoint.
/// </summary>
[ProtoContract]
public class TestWebhookResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the test was successful.
    /// </summary>
    [ProtoMember(1)]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the test failed.
    /// </summary>
    [ProtoMember(2)]
    public string ErrorMessage { get; set; } = string.Empty;
}
