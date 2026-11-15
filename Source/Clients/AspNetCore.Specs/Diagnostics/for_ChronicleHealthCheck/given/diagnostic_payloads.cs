// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Chronicle.AspNetCore.Diagnostics.for_ChronicleHealthCheck.given;

public static class diagnostic_payloads
{
    public static IEnumerable<string> Text(object? value)
    {
        if (value is null)
        {
            yield break;
        }

        yield return value.ToString() ?? string.Empty;
        if (value is string)
        {
            yield break;
        }

        foreach (var text in CollectionText(value).Concat(value is Exception exception ? ExceptionText(exception) : []))
        {
            yield return text;
        }
    }

    public static void ShouldNotExposeMarkers(IEnumerable<string> values)
    {
        string[] markers = ["synthetic-health-user-marker", "1753198871", "175319", "synthetic-health-tail-marker", "synthetic-health-certificate-marker", "synthetic-health-setting-marker"];
        foreach (var value in values)
        {
            foreach (var marker in markers)
            {
                value.Contains(marker, StringComparison.Ordinal).ShouldBeFalse();
            }
        }
    }

    static IEnumerable<string> CollectionText(object value) => value switch
    {
        IDictionary dictionary => DictionaryText(dictionary),
        IEnumerable items => items.Cast<object?>().SelectMany(Text),
        _ => []
    };

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

    static IEnumerable<string> ExceptionText(Exception exception) =>
        new[] { exception.Message }
            .Concat(Text(exception.Data))
            .Concat(StringProperties(exception))
            .Concat(Causes(exception).SelectMany(Text));

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
