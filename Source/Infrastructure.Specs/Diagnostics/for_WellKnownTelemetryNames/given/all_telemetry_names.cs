// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Diagnostics.for_WellKnownTelemetryNames.given;

public class all_telemetry_names : Specification
{
    protected IDictionary<string, string> _names = new Dictionary<string, string>();

    void Establish() => _names = Collect(typeof(WellKnownTelemetryAttributes), "Attributes.")
        .Concat(Collect(typeof(WellKnownMetricNames), "Metrics."))
        .Concat(Collect(typeof(WellKnownSpanNames), "Spans."))
        .Concat(Collect(typeof(WellKnownClientSpanNames), "ClientSpans."))
        .ToDictionary(_ => _.Key, _ => _.Value);

    protected static bool IsUnit(string path) => path.StartsWith("Metrics.") && path.EndsWith("Unit");

    protected IEnumerable<KeyValuePair<string, string>> In(string group) =>
        _names.Where(_ => _.Key.StartsWith($"{group}.") && !IsUnit(_.Key));

    static Dictionary<string, string> Collect(Type type, string prefix)
    {
        var result = new Dictionary<string, string>();
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(_ => _.IsLiteral))
        {
            result[$"{prefix}{field.Name}"] = (string)field.GetRawConstantValue()!;
        }

        return result;
    }
}
