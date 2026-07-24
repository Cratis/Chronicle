// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

internal static partial class UniqueConstraintsStorageLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Unique index could not be created on constraint collection '{Collection}' because it already contains duplicate values. Falling back to a non-unique index; the pre-existing duplicates should be reconciled.")]
    internal static partial void FallingBackToNonUniqueIndex(this ILogger<UniqueConstraintsStorage> logger, string collection);
}
