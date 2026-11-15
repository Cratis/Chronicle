// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Represents how many failures of one kind <see cref="RepeatedFailureLog"/> counted instead of writing.
/// </summary>
/// <param name="Category">The logger category the failures came from, or <see cref="RepeatedFailureLog.OverflowCategory"/> for failures of kinds that were not tracked.</param>
/// <param name="Message">The message template of the failures; empty for failures of kinds that were not tracked.</param>
/// <param name="Exception">The type of the exception at the root of the failures.</param>
/// <param name="Suppressed">How many failures were counted instead of written.</param>
/// <param name="Level">The highest level of the failures that were counted.</param>
public readonly record struct SuppressedFailures(string Category, string Message, Type Exception, int Suppressed, LogLevel Level)
{
    /// <summary>
    /// Gets whether the failures were of kinds that were not tracked separately, because too many kinds were failing at once.
    /// </summary>
    public bool IsUntracked => Category == RepeatedFailureLog.OverflowCategory;
}
