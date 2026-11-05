// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>
/// Log messages for <see cref="EncryptionKeyStorage"/>.
/// </summary>
internal static partial class EncryptionKeyStorageLogging
{
    [LoggerMessage(LogLevel.Warning, "Encryption key identifiers stored before exact comparison are awaiting review: {Count} rows in EncryptionKeyIdentifierReviews")]
    internal static partial void IdentifiersAwaitingReview(this ILogger logger, int count);

    [LoggerMessage(LogLevel.Warning, "An encryption key operation was refused because its identifier is under review; see the compliance storage documentation")]
    internal static partial void IdentifierUnderReview(this ILogger logger);
}
