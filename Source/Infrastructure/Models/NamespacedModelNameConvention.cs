// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Strings;
using Humanizer;

namespace Cratis.Models;

/// <summary>
/// Represents an implementation of <see cref="IModelNameConvention"/> that uses the namespace of the model type as a prefix.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NamespacedModelNameConvention"/> class.
/// </remarks>
/// <param name="segmentsToSkip">Optionally number of segments in the namespace to skip. Defaults to 0.</param>
/// <param name="separator">Optional separator character to use between namespace segments. Defaults to 0.</param>
/// <param name="prefix">Optional prefix to prepend all collection names with.</param>
public class NamespacedModelNameConvention(int segmentsToSkip = 0, char separator = '-', string prefix = "") : IModelNameConvention
{
    /// <inheritdoc/>
    public string GetNameFor(Type type)
    {
        var segments = type.Namespace?.Split('.') ?? [];
        var modelPrefix = string.Join(separator, segments.Skip(segmentsToSkip).Select(_ => _.ToCamelCase()));
        return $"{prefix}{modelPrefix}-{type.Name.Pluralize().ToCamelCase()}";
    }
}
