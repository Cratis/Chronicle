// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security;

internal static partial class EncryptionLogging
{
    [LoggerMessage(LogLevel.Warning, "A value was decrypted with the previous encryption certificate '{KeyId}'. That certificate is still required - retiring it now makes this value unreadable. Values are re-protected with the active certificate when they are next written")]
    internal static partial void ValueDecryptedWithPreviousCertificate(this ILogger<Encryption> logger, string keyId);
}
