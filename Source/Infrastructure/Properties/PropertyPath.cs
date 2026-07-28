// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Reflection;
using System.Text.RegularExpressions;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Objects;
using Cratis.Reflection;
using Cratis.Strings;

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Represents an encapsulation of a property in the system - used for accessing properties on objects.
/// </summary>
/// <remarks>
/// <see cref="PropertyPath"/> is an immutable type. Every operation performed on it will return a new instance.
/// </remarks>
public partial class PropertyPath
{
    /// <summary>
    /// Represents the not set value.
    /// </summary>
    public const string NotSetValue = "*NotSet*";

    /// <summary>
    /// Represents the this accessor.
    /// </summary>
    public const string ThisAccessorValue = "$this";

    /// <summary>
    /// Represents the root path.
    /// </summary>
    public static readonly PropertyPath Root = new(string.Empty);

    /// <summary>
    /// Get the value that identifies a <see cref="PropertyPath"/> that is not set.
    /// </summary>
    public static readonly PropertyPath NotSet = NotSetValue;

    readonly IPropertyPathSegment[] _segments = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyPath"/> class.
    /// </summary>
    /// <param name="path">Path to the property relative within an object.</param>
    public PropertyPath(string path)
    {
        _segments = SegmentsFrom(path);
        Path = Render(_segments);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyPath"/> class from segments that are already resolved.
    /// </summary>
    /// <param name="segments">The resolved segments the path consists of.</param>
    /// <param name="path">The rendering of <paramref name="segments"/>.</param>
    /// <remarks>
    /// Rendering a segment always parses back to that same segment, so composing resolved segments is equivalent to
    /// parsing the rendered path again - only without the split, regular expression and join it would cost.
    /// </remarks>
    PropertyPath(IPropertyPathSegment[] segments, string path)
    {
        _segments = segments;
        Path = path;
    }

    /// <summary>
    /// Gets the full path of the property.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the segments the full property path consists of.
    /// </summary>
    public IEnumerable<IPropertyPathSegment> Segments => _segments;

    /// <summary>
    /// Gets the last segment of the path.
    /// </summary>
    public IPropertyPathSegment LastSegment => _segments[^1];

    /// <summary>
    /// Gets whether or not this is the root path.
    /// </summary>
    public bool IsRoot => Path == Root;

    /// <summary>
    /// Gets whether or not the value is set.
    /// </summary>
    public bool IsSet => !string.IsNullOrEmpty(Path) && !Path.Equals(NotSetValue);

    /// <summary>
    /// Implicitly convert from <see cref="PropertyPath"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="property"><see cref="PropertyPath"/> to convert from.</param>
    /// <returns>Converted path.</returns>
    public static implicit operator string(PropertyPath property) => property?.Path ?? string.Empty;

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to a <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="path">The path of the property.</param>
    /// <returns>Converted <see cref="PropertyPath"/>.</returns>
    public static implicit operator PropertyPath(string path) => new(path);

    /// <summary>
    /// Operator overload for equality comparison.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if they're equal, false if not.</returns>
    public static bool operator ==(PropertyPath left, PropertyPath right) => left.Equals(right);

    /// <summary>
    /// Operator overload for not-equality comparison.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if they're not equal, false if they are.</returns>
    public static bool operator !=(PropertyPath left, PropertyPath right) => !left.Equals(right);

    /// <summary>
    /// Adds two <see cref="PropertyPath"/> together - formatting it correctly.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>A merged <see cref="PropertyPath"/>.</returns>
    public static PropertyPath operator +(PropertyPath left, PropertyPath right)
    {
        if (right.Path.Length == 0)
        {
            return new([.. left._segments], left.Path);
        }

        if (left.Path.Length == 0)
        {
            return new([.. right._segments], right.Path);
        }

        return new([.. left._segments, .. right._segments], $"{left.Path}.{right.Path}");
    }

    /// <summary>
    /// Add a <see cref="IPropertyPathSegment"/> to a <see cref="PropertyPath"/> and return a new instance.
    /// </summary>
    /// <param name="left"><see cref="PropertyPath"/> to add to.</param>
    /// <param name="segment"><see cref="IPropertyPathSegment"/> to add.</param>
    /// <returns>New <see cref="PropertyPath"/>.</returns>
    public static PropertyPath operator +(PropertyPath left, IPropertyPathSegment segment)
    {
        return segment switch
        {
            PropertyName => left.AddProperty(segment.Value),
            ArrayProperty => left.AddArrayIndex(segment.Value),
            ThisAccessor => left.AddThisAccessor(),
            _ => left
        };
    }

    /// <summary>
    /// Create a new <see cref="PropertyPath"/> from segments.
    /// </summary>
    /// <param name="segments">Segments to initialize it with.</param>
    /// <returns>A new <see cref="PropertyPath"/> instance.</returns>
    public static PropertyPath CreateFrom(IPropertyPathSegment[] segments)
    {
        var current = Root;
        foreach (var segment in segments)
        {
            current += segment;
        }

        return current;
    }

    /// <summary>
    /// Add an <see cref="PropertyName"/> as segment by creating a new <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="name">Name of the property.</param>
    /// <param name="type">Optional type. It will use this to determine things like if it is an array or not.</param>
    /// <returns>A new <see cref="PropertyPath"/> with the segment appended.</returns>
    /// <remarks>This operation does not mutate the original.</remarks>
    public PropertyPath AddProperty(string name, Type? type = null)
    {
        if (type?.IsEnumerable() == true && type?.IsAssignableTo(typeof(IDictionary)) == false)
        {
            name = $"[{name}]]";
        }

        return Append(SegmentsFrom(name));
    }

    /// <summary>
    /// Add an <see cref="ArrayProperty"/> as segment by creating a new <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="identifier">Identifier of the array segment.</param>
    /// <returns>A new <see cref="PropertyPath"/> with the segment appended.</returns>
    /// <remarks>This operation does not mutate the original.</remarks>
    public PropertyPath AddArrayIndex(string identifier)
    {
        var identifierSegments = SegmentsFrom(identifier);
        var added = new IPropertyPathSegment[identifierSegments.Length];
        for (var index = 0; index < identifierSegments.Length - 1; index++)
        {
            added[index] = ResolvePropertyPathSegment(identifierSegments[index].Value);
        }

        added[^1] = ResolvePropertyPathSegment($"[{identifierSegments[^1].Value}]");
        return Append(added);
    }

    /// <summary>
    /// Adds a <see cref="ThisAccessorValue"/> as segment by creating a new <see cref="PropertyPath"/>.
    /// </summary>
    /// <returns>A new <see cref="PropertyPath"/> with the segment appended.</returns>
    /// <remarks>This operation does not mutate the original.</remarks>
    public PropertyPath AddThisAccessor() => new([.. _segments, new ThisAccessor()], $"{Path}.{ThisAccessorValue}");

    /// <summary>
    /// Check whether or not there is a value at the path of the property for a specific target.
    /// </summary>
    /// <param name="target">Object to get from.</param>
    /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
    /// <returns>Value, if any.</returns>
    public bool HasValue(object target, ArrayIndexers arrayIndexers)
    {
        if (target is ExpandoObject targetAsExpandoObject)
        {
            var innerInstance = targetAsExpandoObject.EnsurePath(this, arrayIndexers) as IDictionary<string, object>;
            return innerInstance.ContainsKey(LastSegment.Value);
        }

        var inner = target.EnsurePath(this, arrayIndexers);
        var propertyInfo = GetPropertyInfoFor(target.GetType());
        return propertyInfo.GetValue(inner) != null;
    }

    /// <summary>
    /// Gets the value at the path of the property.
    /// </summary>
    /// <param name="target">Object to get from.</param>
    /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
    /// <returns>Value, if any.</returns>
    public object? GetValue(object target, ArrayIndexers arrayIndexers)
    {
        if (target is ExpandoObject targetAsExpandoObject)
        {
            var innerInstance = targetAsExpandoObject.EnsurePath(this, arrayIndexers) as IDictionary<string, object>;
            return innerInstance.TryGetValue(LastSegment.Value, out var value) ? value : null;
        }

        var inner = target.EnsurePath(this, arrayIndexers);
        var propertyInfo = GetPropertyInfoFor(target.GetType());
        return propertyInfo.GetValue(inner);
    }

    /// <summary>
    /// Set a specific value at the path of the property.
    /// </summary>
    /// <param name="target">Object to set to.</param>
    /// <param name="value">Value to set.</param>
    /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
    public void SetValue(object target, object value, ArrayIndexers arrayIndexers)
    {
        if (target is ExpandoObject targetAsExpandoObject)
        {
            var inner = targetAsExpandoObject.EnsurePath(this, arrayIndexers) as IDictionary<string, object>;
            inner[LastSegment.Value] = value;
        }
        else
        {
            var inner = target.EnsurePath(this, arrayIndexers);
            var propertyInfo = GetPropertyInfoFor(target.GetType());
            propertyInfo.SetValue(inner, value);
        }
    }

    /// <summary>
    /// Get the corresponding <see cref="PropertyInfo"/> for the full path from the root type.
    /// </summary>
    /// <typeparam name="TRoot">Type of root.</typeparam>
    /// <returns>The <see cref="PropertyInfo"/>.</returns>
    /// <exception cref="UnableToResolvePropertyPathOnType">Thrown if not able to resolve the property path on the type.</exception>
    public PropertyInfo GetPropertyInfoFor<TRoot>() => GetPropertyInfoFor(typeof(TRoot));

    /// <summary>
    /// Get the corresponding <see cref="PropertyInfo"/> for the full path from the root type.
    /// </summary>
    /// <param name="rootType">Type of root.</param>
    /// <returns>The <see cref="PropertyInfo"/>.</returns>
    /// <exception cref="UnableToResolvePropertyPathOnType">Thrown if not able to resolve the property path on the type.</exception>
    public PropertyInfo GetPropertyInfoFor(Type rootType)
    {
        var currentType = rootType;
        PropertyInfo? currentPropertyInfo = null;

        foreach (var segment in Segments)
        {
            if (currentType is null) break;

            currentPropertyInfo =
                currentType.GetProperty(segment.Value, BindingFlags.Public | BindingFlags.Instance) ??
                currentType.GetProperty(segment.Value.ToPascalCase(), BindingFlags.Public | BindingFlags.Instance);

            currentType = currentPropertyInfo?.PropertyType;
        }

        if (currentPropertyInfo is null)
        {
            throw new UnableToResolvePropertyPathOnType(rootType, this);
        }

        return currentPropertyInfo;
    }

    /// <inheritdoc/>
    public override string ToString() => Path;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => (obj as PropertyPath)?.Path.Equals(Path) ?? false;

    /// <inheritdoc/>
    public override int GetHashCode() => Path.GetHashCode();

#pragma warning disable MA0190
    [GeneratedRegex("\\[(?<property>[\\w-_]*)\\]", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    internal static partial Regex ArrayIndexRegexGenerator();
#pragma warning restore MA0190

    static string Render(IPropertyPathSegment[] segments) => string.Join('.', (IEnumerable<object>)segments);

    static IPropertyPathSegment[] SegmentsFrom(string path)
    {
        var parts = path.Split('.');
        var segments = new IPropertyPathSegment[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            segments[index] = ResolvePropertyPathSegment(parts[index]);
        }

        return segments;
    }

    static IPropertyPathSegment ResolvePropertyPathSegment(string segment)
    {
        var match = ArrayIndexRegexGenerator().Match(segment);
        if (match.Success)
        {
            return new ArrayProperty(match.Groups["property"].Value);
        }
        if (segment == ThisAccessorValue)
        {
            return new ThisAccessor();
        }
        return new PropertyName(segment);
    }

    PropertyPath Append(IPropertyPathSegment[] segments)
    {
        var appendedPath = Render(segments);
        return Path.Length == 0
            ? new(segments, appendedPath)
            : new([.. _segments, .. segments], $"{Path}.{appendedPath}");
    }
}
