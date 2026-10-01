// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Defines the alert settings in effect: the configuration merged over the built-in defaults.
/// </summary>
public interface IAlertConditions
{
    /// <summary>
    /// Gets the settings in effect for a condition.
    /// </summary>
    /// <param name="kind">The <see cref="AlertConditionKind"/> to get the settings for.</param>
    /// <returns>The <see cref="AlertCondition"/> in effect. It is disabled whenever alerts as a whole are disabled.</returns>
    /// <exception cref="UnknownAlertCondition">Thrown when <paramref name="kind"/> is not a condition the kernel knows.</exception>
    AlertCondition For(AlertConditionKind kind);

    /// <summary>
    /// Gets whether an observer is left out of alerting by the configured exclusions.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> to check.</param>
    /// <returns>True when a configured identifier or glob pattern matches it; otherwise, false.</returns>
    bool IsExcluded(ObserverId observerId);
}
