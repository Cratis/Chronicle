// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

public static class diagnostic_surfaces
{
    public static string DebuggerText(object value)
    {
        var type = value.GetType();
        var display = type.GetCustomAttribute<DebuggerDisplayAttribute>()!.Value;
        var member = display.Split(',')[0].TrimStart('{');

        return member.EndsWith("()", StringComparison.Ordinal)
            ? (string)type.GetMethod(member[..^2])!.Invoke(value, null)!
            : (string)type.GetProperty(member)!.GetValue(value)!;
    }

    public static IEnumerable<string> ExceptionText(Exception exception) =>
        new[] { exception.Message, exception.ToString() }
            .Concat(DataText(exception.Data))
            .Concat(StringProperties(exception))
            .Concat(Causes(exception).SelectMany(ExceptionText));

    public static void ShouldContainNoMarkers(IEnumerable<string> values)
    {
        string[] markers =
        [
            a_connection_string_corpus.Username,
            a_connection_string_corpus.Password,
            a_connection_string_corpus.NumericPassword,
            a_connection_string_corpus.CertificatePath,
            "synthetic-tail-marker",
            "synthetic-query-key-marker",
            "synthetic-query-value-marker",
            "synthetic-path-marker",
            "synthetic-fragment-marker",
            "175319",
            "synthetic-certificate"
        ];

        foreach (var value in values)
        {
            foreach (var marker in markers)
            {
                value.Contains(marker, StringComparison.Ordinal).ShouldBeFalse();
            }
        }
    }

    static IEnumerable<string> DataText(IDictionary data)
    {
        foreach (DictionaryEntry entry in data)
        {
            yield return entry.Key.ToString() ?? string.Empty;
            yield return entry.Value?.ToString() ?? string.Empty;
        }
    }

    static IEnumerable<string> StringProperties(Exception exception) => exception.GetType().GetProperties()
        .Where(property => property.GetIndexParameters().Length == 0 && property.PropertyType == typeof(string))
        .Select(property => (string?)property.GetValue(exception) ?? string.Empty);

    static IEnumerable<Exception> Causes(Exception exception) => exception switch
    {
        AggregateException aggregate => aggregate.InnerExceptions,
        { InnerException: { } inner } => [inner],
        _ => []
    };
}
