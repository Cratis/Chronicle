// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Logs the shared stop budget.
/// </summary>
internal static partial class ShutdownDrainBudgetLogging
{
    /// <summary>
    /// Logs the effective drain timeout at startup.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="timeout">The effective timeout.</param>
    [LoggerMessage(LogLevel.Information, "Effective shared shutdown drain budget is {Timeout}")]
    internal static partial void EffectiveDrainBudget(this ILogger<ShutdownDrainBudget> logger, TimeSpan timeout);
}
