// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents the outcome of testing a webhook endpoint or the authorization it is called with.
/// </summary>
public class WebhookTestResult
{
    /// <summary>
    /// Gets or sets whether the endpoint answered.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets what went wrong, empty when nothing did.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
