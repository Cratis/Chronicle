// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Compliance;

/// <summary>
/// Log messages for <see cref="ComplianceService"/>.
/// </summary>
internal static partial class ComplianceServiceLogMessages
{
    [LoggerMessage(LogLevel.Error, "Failed to release compliance for event store '{EventStore}' namespace '{Namespace}' subject '{Subject}'")]
    internal static partial void FailedToRelease(this ILogger<ComplianceService> logger, string eventStore, string @namespace, string subject, Exception exception);
}
