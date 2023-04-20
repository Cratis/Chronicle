// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Strings;
using Humanizer;

namespace Aksio.Cratis.Models;

/// <summary>
/// Represents an implementation of <see cref="IModelNameConvention"/> that uses the namespace of the model type as a prefix.
/// </summary>
public class NamespacedModelNameConvention : IModelNameConvention
{
    readonly int _segmentsToSkip;
    readonly char _separator;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamespacedModelNameConvention"/> class.
    /// </summary>
    /// <param name="segmentsToSkip">Optionally number of segments in the namespace to skip. Defaults to 0.</param>
    /// <param name="separator">Optional separator character to use between namespace segments. Defaults to 0.</param>
    public NamespacedModelNameConvention(int segmentsToSkip = 0, char separator = '-')
    {
        _segmentsToSkip = segmentsToSkip;
        _separator = separator;
    }

    /// <inheritdoc/>
    public string GetNameFor(Type type)
    {
        var segments = type.Namespace?.Split('.') ?? Array.Empty<string>();
        var prefix = string.Join(_separator, segments.Skip(_segmentsToSkip).Select(_ => _.ToCamelCase()));
        return $"{prefix}-{type.Name.Pluralize().ToCamelCase()}";
    }
}
