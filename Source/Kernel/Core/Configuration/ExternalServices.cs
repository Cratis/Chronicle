// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents destination validation settings for HTTP capture sources.
/// </summary>
public class ExternalServices : IOutboundTargetSettings
{
    /// <summary>
    /// Gets the destination validation mode.
    /// </summary>
#if DEVELOPMENT
    public WebhookValidationMode Validation { get; init; } = WebhookValidationMode.Warn;
#else
    public WebhookValidationMode Validation { get; init; } = WebhookValidationMode.BlockLocalAddresses;
#endif

    /// <summary>
    /// Gets whether HTTP is allowed in Development. Ignored outside Development.
    /// </summary>
    public bool AllowInsecureHttp { get; init; }

    /// <summary>
    /// Gets the globally allowed hosts, with exact hosts or wildcard subdomains such as *.example.com.
    /// </summary>
    public IEnumerable<string> AllowedHosts { get; init; } = [];

    /// <summary>
    /// Gets host allow-lists keyed by event-store name. An entry replaces the global list.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> EventStoreAllowedHosts { get; init; } = new Dictionary<string, string[]>();
}
