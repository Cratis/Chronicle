// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Orleans.StateMachines;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class StateMachineLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Transitioning to {StateType}")]
    internal static partial void TransitioningTo(this ILogger<StateMachine<object>> logger, Type stateType);
}
