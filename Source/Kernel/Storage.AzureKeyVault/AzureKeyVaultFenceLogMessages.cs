// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>Diagnostics for the database authority and release-N compatibility writes.</summary>
internal static partial class AzureKeyVaultFenceLogMessages
{
    /// <summary>Warn that erasure state cannot survive process loss.</summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(0, LogLevel.Warning, "Azure Key Vault encryption key storage has no durable database fence authority; fence guarantees are None.")]
    internal static partial void NoDurableAuthority(this ILogger logger);

    /// <summary>Report a best-effort compatibility write failure.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The write failure.</param>
    [LoggerMessage(1, LogLevel.Warning, "Could not update a legacy Azure Key Vault fence; the database remains authoritative.")]
    internal static partial void LegacyWriteFailed(this ILogger logger, Exception exception);
}
