// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

internal static partial class ConstraintsByBuilderProviderLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Failed to activate constraint of type '{ConstraintType}' - skipping constraint")]
    internal static partial void FailedToActivateConstraint(this ILogger logger, Type constraintType, Exception exception);
}
