// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Cratis.Chronicle;

/// <summary>
/// Stamps the value of a property (or record parameter) marked with <see cref="AudienceAttribute"/> as the audience tag.
/// </summary>
internal sealed class AudiencePropertyEventTagEnricher : IEventTagEnricher
{
    static readonly ConcurrentDictionary<Type, PropertyInfo[]> _properties = new();

    /// <inheritdoc/>
    public IEnumerable<NamedTag> GetTags(EventTagContext context)
    {
        var tags = new List<NamedTag>();
        foreach (var property in _properties.GetOrAdd(context.Event.GetType(), FindAudienceProperties))
        {
            var value = property.GetValue(context.Event);
            if (value is null) continue;

            if (value.IsConcept()) value = value.GetConceptValue();
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(text)) tags.Add(new NamedTag(new TagName(WellKnownTags.Audience), text));
        }

        return tags;
    }

    static PropertyInfo[] FindAudienceProperties(Type type)
    {
        var markedParameters = type.GetConstructors()
            .SelectMany(_ => _.GetParameters())
            .Where(_ => Attribute.IsDefined(_, typeof(AudienceAttribute)))
            .Select(_ => _.Name)
            .ToHashSet();

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(_ => _.GetIndexParameters().Length == 0 && _.CanRead && (Attribute.IsDefined(_, typeof(AudienceAttribute)) || markedParameters.Contains(_.Name)))
            .ToArray();
    }
}
