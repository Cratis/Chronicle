// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Extensions.Orleans.Observers;

/// <summary>
/// Holds log messages for <see cref="ObserverManager{T}"/>.
/// </summary>
public static partial class ObserverManagerLogMessages
{
    [LoggerMessage(0, LogLevel.Debug, "{Prefix}: Updating entry for {Address}/{Observer}. {Count} total subscribers.")]
    internal static partial void UpdatingEntry(this ILogger logger, string prefix, object address, object observer, int count);

    [LoggerMessage(1, LogLevel.Debug, "{Prefix}: Adding entry for {Address}/{Observer}. {Count} total subscribers after add.")]
    internal static partial void AddingEntry(this ILogger logger, string prefix, object address, object observer, int count);

    [LoggerMessage(2, LogLevel.Debug, "{Prefix}: Removing entry for {Address}. {Count} total subscribers after remove.")]
    internal static partial void RemovingEntry(this ILogger logger, string prefix, object address, int count);

    [LoggerMessage(3, LogLevel.Debug, "{Prefix}: Removing defunct entry for {Address}. {Count} total subscribers after remove.")]
    internal static partial void RemovingDefunctEntry(this ILogger logger, string prefix, object address, int count);
}
