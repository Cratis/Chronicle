// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Enumeration;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents an implementation of <see cref="IAlertConditions"/> that merges the <c language="csharp">alerts</c> configuration over
/// the built-in defaults.
/// </summary>
/// <remarks>
/// The defaults are <c language="csharp">partition-failing</c> as a warning after five minutes, and <c language="csharp">partition-retries-exhausted</c>
/// and <c language="csharp">observer-quarantined</c> as critical. The configuration is read on every call, so a change to it applies to
/// the next evaluation.
/// </remarks>
/// <param name="options">The <see cref="IOptionsMonitor{TOptions}"/> for <see cref="ChronicleOptions"/>.</param>
[Singleton]
public class AlertConditions(IOptionsMonitor<ChronicleOptions> options) : IAlertConditions
{
    static readonly TimeSpan _defaultRaiseAfter = TimeSpan.FromMinutes(5);

    static readonly Dictionary<AlertConditionKind, AlertSeverity> _defaultSeverities = new()
    {
        [AlertConditionKind.PartitionFailing] = AlertSeverity.Warning,
        [AlertConditionKind.PartitionRetriesExhausted] = AlertSeverity.Critical,
        [AlertConditionKind.ObserverQuarantined] = AlertSeverity.Critical
    };

    /// <inheritdoc/>
    public AlertCondition For(AlertConditionKind kind)
    {
        if (!_defaultSeverities.TryGetValue(kind, out var defaultSeverity))
        {
            throw new UnknownAlertCondition(kind);
        }

        var alerts = options.CurrentValue.Alerts;

        // Looked up without regard to case whatever comparer the configured dictionary was created with.
        var configured = alerts.Conditions
            .Where(_ => string.Equals(_.Key, kind.Value, StringComparison.OrdinalIgnoreCase))
            .Select(_ => _.Value)
            .FirstOrDefault();
        if (configured is null)
        {
            return new(kind, alerts.Enabled, defaultSeverity, _defaultRaiseAfter);
        }

        return new(kind, alerts.Enabled && configured.Enabled, configured.Severity ?? defaultSeverity, configured.RaiseAfter);
    }

    /// <inheritdoc/>
    public bool IsExcluded(ObserverId observerId) =>
        options.CurrentValue.Alerts.ExcludeObservers.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, observerId.Value, false));
}
