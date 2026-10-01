// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for one alert condition.
/// </summary>
public class AlertConditionOptions
{
    /// <summary>
    /// Gets whether the condition raises alerts. A disabled condition raises nothing new; an incident that is already
    /// open still clears normally.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the severity to raise the condition with, or null to keep the built-in default of the condition.
    /// </summary>
    public AlertSeverity? Severity { get; init; }

    /// <summary>
    /// Gets how long a partition has to keep failing, while still being retried, before <c language="csharp">partition-failing</c>
    /// raises. Must be non-negative; zero raises immediately. It means nothing for the other conditions.
    /// </summary>
    public TimeSpan RaiseAfter { get; init; } = TimeSpan.FromMinutes(5);
}
