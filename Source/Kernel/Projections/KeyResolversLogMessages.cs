// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Holds log messages for <see cref="KeyResolvers"/>.
/// </summary>
internal static partial class KeyResolversLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Resolving key using key resolver: {KeyResolverName}")]
    internal static partial void ResolvingKey(this ILogger<KeyResolvers> logger, string keyResolverName);
    [LoggerMessage(LogLevel.Warning, "An error occurred while resolving the key resolver: {KeyResolverName}")]
    internal static partial void ErrorResolving(this ILogger<KeyResolvers> logger, Exception ex, string keyResolverName);
}