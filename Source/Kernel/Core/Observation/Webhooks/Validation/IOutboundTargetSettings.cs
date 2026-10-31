// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// Defines destination validation settings for outbound HTTP requests.
/// </summary>
public interface IOutboundTargetSettings
{
    /// <summary>
    /// Gets the validation mode.
    /// </summary>
    WebhookValidationMode Validation { get; }

    /// <summary>
    /// Gets a value indicating whether insecure HTTP is allowed.
    /// </summary>
    bool AllowInsecureHttp { get; }

    /// <summary>
    /// Gets the globally allowed hosts.
    /// </summary>
    IEnumerable<string> AllowedHosts { get; }

    /// <summary>
    /// Gets the allowed hosts for individual event stores.
    /// </summary>
    IReadOnlyDictionary<string, string[]> EventStoreAllowedHosts { get; }
}
