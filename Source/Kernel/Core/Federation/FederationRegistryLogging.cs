// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Holds log messages for <see cref="FederationRegistry"/>.
/// </summary>
internal static partial class FederationRegistryLogging
{
    [LoggerMessage(LogLevel.Warning, "Skipping configured federation binding '{BindingId}': {Reason}")]
    internal static partial void SkippingInvalidConfiguredBinding(this ILogger<FederationRegistry> logger, string bindingId, string reason);

    [LoggerMessage(LogLevel.Warning, "Skipping configured federation grant '{LinkId}': {Reason}")]
    internal static partial void SkippingInvalidConfiguredGrant(this ILogger<FederationRegistry> logger, string linkId, string reason);

    [LoggerMessage(LogLevel.Information, "Bootstrapped federation binding '{BindingId}' from configuration")]
    internal static partial void BootstrappedBinding(this ILogger<FederationRegistry> logger, string bindingId);

    [LoggerMessage(LogLevel.Information, "Bootstrapped federation grant '{LinkId}' from configuration")]
    internal static partial void BootstrappedGrant(this ILogger<FederationRegistry> logger, string linkId);
}
