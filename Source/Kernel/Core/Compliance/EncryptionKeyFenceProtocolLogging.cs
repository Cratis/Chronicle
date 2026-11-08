// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Compliance;

internal static partial class EncryptionKeyFenceProtocolLogging
{
    [LoggerMessage(LogLevel.Warning, "Release N enforces atomic encryption key fencing. Old silos can still overwrite fences: erasure confirmation is unsafe until every silo runs N or later. ProtocolFloor attestation is required before Release N+1.")]
    internal static partial void RolloutWarning(this ILogger<EncryptionKeyFenceProtocol> logger);

    [LoggerMessage(LogLevel.Information, "Encryption key storage {StorageType} advertises {Guarantees}.")]
    internal static partial void StorageCapability(this ILogger<EncryptionKeyFenceProtocol> logger, string storageType, EncryptionKeyFenceGuarantees guarantees);

    [LoggerMessage(LogLevel.Information, "Encryption key revocation grace is {Grace}.")]
    internal static partial void RevocationGrace(this ILogger<EncryptionKeyFenceProtocol> logger, TimeSpan grace);

    [LoggerMessage(LogLevel.Information, "Legacy/unsealed encryption key fence count in an authority scope: {Count}.")]
    internal static partial void LegacyFences(this ILogger<EncryptionKeyFenceProtocol> logger, long? count);

    [LoggerMessage(LogLevel.Warning, "Encryption key protocol publication is unavailable; no durable protocol record was acknowledged.")]
    internal static partial void ProtocolPublicationFailed(this ILogger<EncryptionKeyFenceProtocol> logger);

    [LoggerMessage(LogLevel.Warning, "The configured encryption key fence authority does not support durable protocol records.")]
    internal static partial void MissingProtocolAuthority(this ILogger<EncryptionKeyFenceProtocol> logger);
}
