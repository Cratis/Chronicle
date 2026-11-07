// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

public static class diagnostic_payloads
{
    public static IEnumerable<string> Text(object? value)
    {
        if (value is null)
        {
            yield break;
        }

        yield return value.ToString() ?? string.Empty;
        if (value is not Exception exception)
        {
            yield break;
        }

        foreach (var text in ExceptionText(exception))
        {
            yield return text;
        }
    }

    public static void ShouldNotExpose(IEnumerable<string> values, params string[] markers)
    {
        foreach (var value in values)
        {
            foreach (var marker in markers)
            {
                value.Contains(marker, StringComparison.Ordinal).ShouldBeFalse();
            }
        }
    }

    static IEnumerable<string> ExceptionText(Exception exception) =>
        new[] { exception.Message }
            .Concat(DictionaryText(exception.Data))
            .Concat(StringProperties(exception))
            .Concat(Causes(exception).SelectMany(Text));

    static IEnumerable<string> DictionaryText(IDictionary dictionary)
    {
        foreach (DictionaryEntry entry in dictionary)
        {
            foreach (var text in Text(entry.Key).Concat(Text(entry.Value)))
            {
                yield return text;
            }
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
