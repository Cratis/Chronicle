// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Schema;

internal static partial class ReadModelSchemaVerificationLogging
{
    [LoggerMessage(LogLevel.Warning, "The read models of quarantined event stores and namespaces were not verified: {Skipped}. They are verified on a later start, once the quarantine is resolved")]
    internal static partial void ReadModelsNotVerified(this ILogger<ReadModelSchemaVerification> logger, string skipped);
}
