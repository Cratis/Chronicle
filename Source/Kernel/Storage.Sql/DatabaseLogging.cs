// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Log messages for <see cref="Database"/>.
/// </summary>
internal static partial class DatabaseLogging
{
    [LoggerMessage(LogLevel.Warning, "Encryption key identifiers stored before exact comparison are awaiting review: {Count} rows in EncryptionKeyIdentifierReviews. See the compliance storage documentation")]
    internal static partial void IdentifiersAwaitingReview(ILogger logger, int count);
}
