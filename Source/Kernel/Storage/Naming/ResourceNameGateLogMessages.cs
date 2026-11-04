// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Holds log messages for <see cref="ResourceNameGate"/>.
/// </summary>
internal static partial class ResourceNameGateLogMessages
{
    [LoggerMessage(LogLevel.Warning, "A name was refused because it breaks the resource name rule ({Violation}): '{Name}'")]
    internal static partial void NameRefused(this ILogger<ResourceNameGate> logger, ResourceNameViolation violation, string name);

    [LoggerMessage(LogLevel.Warning, "A name was refused because it conflicts with an existing name ({Conflict}): '{Name}'")]
    internal static partial void NameConflict(this ILogger<ResourceNameGate> logger, ResourceNameConflictKind conflict, string name);

    [LoggerMessage(LogLevel.Warning, "A name was refused because it is quarantined: '{Name}'")]
    internal static partial void NameQuarantined(this ILogger<ResourceNameGate> logger, string name);
}
