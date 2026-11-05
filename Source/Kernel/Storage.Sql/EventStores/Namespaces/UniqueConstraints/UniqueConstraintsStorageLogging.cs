// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Log messages for <see cref="UniqueConstraintsStorage"/>.
/// </summary>
internal static partial class UniqueConstraintsStorageLogging
{
    [LoggerMessage(LogLevel.Warning, "{Count} unique constraint rows in {EventSequenceId} were written before owners were compared exactly and stay pending until the constraint index is rebuilt")]
    internal static partial void ConstraintRowsPendingReview(this ILogger<UniqueConstraintsStorage> logger, int count, string eventSequenceId);
}
