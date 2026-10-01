// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Alerts;

internal static partial class AlertConditionsLogging
{
    [LoggerMessage(LogLevel.Warning, "Ignoring configured alert condition {Condition}: this kernel version does not support it")]
    internal static partial void IgnoringUnknownCondition(this ILogger<AlertConditions> logger, string condition);
}
