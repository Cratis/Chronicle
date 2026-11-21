// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Log messages of the federation key protection. None of them ever carries key material.
/// </summary>
internal static partial class FederationKeyProtectionLogging
{
    [LoggerMessage(LogLevel.Warning, "Generated a federation key-encryption key at '{Path}' because this is a Development environment. A production kernel never does this: provision the key as a mounted secret.")]
    internal static partial void GeneratedKeyEncryptionKey(this ILogger<FederationKeyProtection> logger, string path);
}
