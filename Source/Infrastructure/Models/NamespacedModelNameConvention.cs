// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Strings;
using Humanizer;

namespace Aksio.Cratis.Models;

/// <summary>
/// Represents an implementation of <see cref="IModelNameConvention"/> that uses the namespace of the model type as a prefix.
/// </summary>
public class NamespacedModelNameConvention : IModelNameConvention
{
    readonly int _segmentsToSkip;
    readonly char _separator;
    readonly string _prefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamespacedModelNameConvention"/> class.
    /// </summary>
    /// <param name="segmentsToSkip">Optionally number of segments in the namespace to skip. Defaults to 0.</param>
    /// <param name="separator">Optional separator character to use between namespace segments. Defaults to 0.</param>
    /// <param name="prefix">Optional prefix to prepend all collection names with.</param>
    public NamespacedModelNameConvention(int segmentsToSkip = 0, char separator = '-', string prefix = "")
    {
        _segmentsToSkip = segmentsToSkip;
        _separator = separator;
        _prefix = prefix;
    }

    /// <inheritdoc/>
    public string GetNameFor(Type type)
    {
        var segments = type.Namespace?.Split('.') ?? Array.Empty<string>();
        var prefix = string.Join(_separator, segments.Skip(_segmentsToSkip).Select(_ => _.ToCamelCase()));
        return $"{_prefix}{prefix}-{type.Name.Pluralize().ToCamelCase()}";
    }
}
