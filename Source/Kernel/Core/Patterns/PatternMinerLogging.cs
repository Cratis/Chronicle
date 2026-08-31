// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patterns;

internal static partial class PatternMinerLogging
{
    [LoggerMessage(LogLevel.Warning, "Failed persisting behavior patterns for {ScopeCount} scopes in event store {EventStore} in namespace {Namespace} - they stay marked for the next flush")]
    internal static partial void FailedPersistingPatterns(this ILogger<PatternMiner> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, int scopeCount, Exception exception);
}
