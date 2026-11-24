// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.Load;

internal static partial class KernelLoadLogMessages
{
    [LoggerMessage(LogLevel.Warning, "The kernel is overloaded and reports itself as degraded: {Reasons} ({ReasonCount} limits crossed)")]
    internal static partial void KernelDegraded(this ILogger<KernelLoad> logger, string reasons, int reasonCount);

    [LoggerMessage(LogLevel.Information, "The kernel's load status changed from {Previous} to {Current}")]
    internal static partial void KernelLoadStatusChanged(this ILogger<KernelLoad> logger, KernelLoadStatus previous, KernelLoadStatus current);
}
