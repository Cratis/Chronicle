// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using System.Reflection;
using Cratis.Serialization;

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Builds typed view instances from the neutral <see cref="GraphRow">rows</see> an <see cref="IGraphQueryExecutor"/> returns.
/// </summary>
/// <remarks>
/// <para>
/// A member is matched to the row by its name after the naming policy, or by its name ignoring case. The constructor with
/// the most parameters is used, then settable properties for members it does not take.
/// </para>
/// <para>
/// Values are converted the way the graph stores them: a concept is built from the value it wraps, a <see cref="Guid"/>
/// from its string, an enum from its name or number, a <see cref="decimal"/> from a number, a <see cref="DateTimeOffset"/>
/// from a timestamp or an ISO 8601 string, a list item by item and a nested view from its row. An absent value gives null,
/// or the default of a non-nullable value type.
/// </para>
/// </remarks>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> member names were given with.</param>
public sealed class GraphRowHydrator(INamingPolicy namingPolicy)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphRowHydrator"/> class with the camelCase naming policy.
    /// </summary>
    public GraphRowHydrator()
        : this(new CamelCaseNamingPolicy())
    {
    }

    /// <summary>
    /// Build a view instance from a row.
    /// </summary>
    /// <typeparam name="TView">The type of the view.</typeparam>
    /// <param name="row">The <see cref="GraphRow"/>, or null.</param>
    /// <returns>The view instance, or null when the row is null.</returns>
    public TView? Hydrate<TView>(GraphRow? row) => (TView?)Hydrate(row, typeof(TView));

    /// <summary>
    /// Build a view instance from a row.
    /// </summary>
    /// <param name="row">The <see cref="GraphRow"/>, or null.</param>
    /// <param name="viewType">The type of the view.</param>
    /// <returns>The view instance, or null when the row is null.</returns>
    public object? Hydrate(GraphRow? row, Type viewType) => row is null ? null : Build(row, viewType);

    /// <summary>
    /// Convert a value as a graph stores it into a CLR type.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="type">The type to convert to.</param>
    /// <returns>The converted value.</returns>
    public object? ConvertValue(object? value, Type type)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (value is null)
        {
            return target.IsValueType && Nullable.GetUnderlyingType(type) is null ? Activator.CreateInstance(target) : null;
        }

        if (value is GraphRow row) return Build(row, target);
        if (target.IsConcept()) return ConceptFactory.CreateConceptInstance(target, ConvertValue(value, target.GetConceptValueType()));
        if (target == typeof(object) || target.IsInstanceOfType(value)) return value;
        if (target == typeof(Guid)) return Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!);
        if (target.IsEnum) return value is string name ? Enum.Parse(target, name, true) : Enum.ToObject(target, value);
        if (target == typeof(DateTimeOffset)) return DateTimeOffsetOf(value);
        if (target == typeof(DateTime)) return DateTimeOffsetOf(value).UtcDateTime;
        if (TryConvertCollection(value, target, out var collection)) return collection;
        return value switch
        {
            string text when target == typeof(DateOnly) => DateOnly.Parse(text, CultureInfo.InvariantCulture),
            string text when target == typeof(TimeOnly) => TimeOnly.Parse(text, CultureInfo.InvariantCulture),
            string text when target == typeof(TimeSpan) => TimeSpan.Parse(text, CultureInfo.InvariantCulture),
            _ => Convert.ChangeType(value, target, CultureInfo.InvariantCulture)
        };
    }

    static DateTimeOffset DateTimeOffsetOf(object value) => value switch
    {
        DateTimeOffset dateTimeOffset => dateTimeOffset,
        DateTime dateTime => new DateTimeOffset(dateTime),
        _ => DateTimeOffset.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
    };

    static Type? ElementTypeOf(Type type)
    {
        if (type == typeof(string)) return null;
        if (type.IsArray) return type.GetElementType();
        if (!type.IsGenericType || type.GetGenericArguments().Length != 1) return null;
        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(List<>) || definition == typeof(IList<>) || definition == typeof(IReadOnlyList<>) || definition == typeof(IEnumerable<>)
            || definition == typeof(IReadOnlyCollection<>) || definition == typeof(ICollection<>)
            ? type.GetGenericArguments()[0]
            : null;
    }

    bool TryConvertCollection(object value, Type target, out object? collection)
    {
        collection = null;
        if (value is string || value is not IEnumerable source || ElementTypeOf(target) is not { } elementType) return false;
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
        foreach (var item in source) list.Add(ConvertValue(item, elementType));
        if (target.IsArray)
        {
            var array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            collection = array;
        }
        else
        {
            collection = list;
        }

        return true;
    }

    object Build(GraphRow row, Type type)
    {
        var constructor = type.GetConstructors().OrderByDescending(_ => _.GetParameters().Length).FirstOrDefault();
        var parameters = constructor?.GetParameters() ?? [];
        var instance = parameters.Length > 0
            ? constructor!.Invoke([.. parameters.Select(parameter => ConvertValue(ValueOf(row, parameter.Name!, parameter.ParameterType), parameter.ParameterType))])
            : Activator.CreateInstance(type)!;

        var settable = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(_ => _.CanWrite && _.GetIndexParameters().Length == 0 && !parameters.Any(parameter => string.Equals(parameter.Name, _.Name, StringComparison.OrdinalIgnoreCase)));
        foreach (var property in settable.Where(_ => HasValue(row, _.Name)))
        {
            property.SetValue(instance, ConvertValue(ValueOf(row, property.Name, property.PropertyType), property.PropertyType));
        }

        return instance;
    }

    bool HasValue(GraphRow row, string clrName) =>
        row.Values.ContainsKey(namingPolicy.GetPropertyName(clrName)) || row.Values.Keys.Any(_ => string.Equals(_, clrName, StringComparison.OrdinalIgnoreCase));

    object? ValueOf(GraphRow row, string clrName, Type type)
    {
        if (row.Values.TryGetValue(namingPolicy.GetPropertyName(clrName), out var value)) return value;
        var key = row.Values.Keys.FirstOrDefault(_ => string.Equals(_, clrName, StringComparison.OrdinalIgnoreCase));
        if (key is not null) return row.Values[key];
        return ElementTypeOf(type) is not null ? Array.Empty<object>() : null;
    }
}
