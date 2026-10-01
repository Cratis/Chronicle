// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents the settings in effect for one alert condition, after the configuration is merged over the defaults.
/// </summary>
/// <param name="Kind">The <see cref="AlertConditionKind"/> the settings are for.</param>
/// <param name="Enabled">Whether the condition raises alerts.</param>
/// <param name="Severity">The <see cref="AlertSeverity"/> to raise the condition with.</param>
/// <param name="RaiseAfter">How long a partition has to keep failing before it raises. Only <see cref="AlertConditionKind.PartitionFailing"/> uses it.</param>
public record AlertCondition(AlertConditionKind Kind, bool Enabled, AlertSeverity Severity, TimeSpan RaiseAfter);
