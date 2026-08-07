// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Holds log messages for <see cref="CompositeEncryptionKeyStorage"/>.
/// </summary>
internal static partial class CompositeEncryptionKeyStorageLogMessages
{
    [LoggerMessage(LogLevel.Information, "Encryption keys are served by a composite of {StoreCount} stores. The first store is read first and is the only one new keys are provisioned on; keys found in a later store are healed into the earlier ones as they are read")]
    internal static partial void Composed(this ILogger<CompositeEncryptionKeyStorage> logger, int storeCount);

    [LoggerMessage(LogLevel.Warning, "Reading the encryption key for '{Identifier}' from one of the composed key stores failed. The remaining stores are still consulted, and the absence of a key is only reported when every store agreed on it")]
    internal static partial void ReadingFromInnerStoreFailed(this ILogger<CompositeEncryptionKeyStorage> logger, EncryptionKeyIdentifier identifier, Exception error);

    [LoggerMessage(LogLevel.Warning, "Healing the encryption key for '{Identifier}' into one of the composed key stores failed. The key was returned to the caller and the store is healed on a later read")]
    internal static partial void HealingInnerStoreFailed(this ILogger<CompositeEncryptionKeyStorage> logger, EncryptionKeyIdentifier identifier, Exception error);

    [LoggerMessage(LogLevel.Warning, "Mirroring the newly provisioned encryption key for '{Identifier}' into one of the composed key stores failed. The key is persisted on the primary store and the mirror is healed on a later read")]
    internal static partial void MirroringToInnerStoreFailed(this ILogger<CompositeEncryptionKeyStorage> logger, EncryptionKeyIdentifier identifier, Exception error);

    [LoggerMessage(LogLevel.Warning, "One of the composed key stores already held a different encryption key for '{Identifier}' than the primary store provisioned. Values protected under one of them cannot be read back through the other - reconcile the stores before completing the cutover")]
    internal static partial void InnerStoreHoldsDivergentKey(this ILogger<CompositeEncryptionKeyStorage> logger, EncryptionKeyIdentifier identifier);
}
