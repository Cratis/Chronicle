// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Collects the properties a model excludes from auto-mapping.
/// </summary>
/// <remarks>
/// One implementation for the root read model, a child record and a nested type. The property- and
/// parameter-level exclusion was collected only for the root, so on a child or a nested type the attribute
/// compiled, emitted no diagnostic and did nothing - a colliding event auto-mapped over the value the author had
/// sourced explicitly, and the natural way to debug it (re-read the attributes, confirm the exclusion is there
/// and spelled right) confirmed the wrong conclusion.
/// </remarks>
public static class NoAutoMapProperties
{
    /// <summary>
    /// Collect the property names a type excludes from auto-mapping.
    /// </summary>
    /// <param name="modelType">The model <see cref="Type"/> to collect from.</param>
    /// <param name="namingPolicy">The <see cref="INamingPolicy"/> the names are converted with.</param>
    /// <returns>The naming-policy-converted property names to exclude.</returns>
    /// <remarks>
    /// Records carry the attribute on the primary constructor's parameters, plain models on properties, so both
    /// are read. The naming-policy conversion has to be the same one the root uses, because the kernel matches
    /// these against a property path's last segment, case-insensitively.
    /// </remarks>
    public static IReadOnlyList<string> CollectFrom(Type? modelType, INamingPolicy namingPolicy)
    {
        if (modelType is null)
        {
            return [];
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var primaryConstructor = modelType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(_ => _.GetParameters().Length)
            .FirstOrDefault();

        foreach (var parameter in primaryConstructor?.GetParameters() ?? [])
        {
            if (parameter.IsDefined(typeof(NoAutoMapAttribute), inherit: true))
            {
                names.Add(namingPolicy.GetPropertyName(new PropertyPath(parameter.Name!)));
            }
        }

        foreach (var property in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (Attribute.IsDefined(property, typeof(NoAutoMapAttribute), inherit: true))
            {
                names.Add(namingPolicy.GetPropertyName(new PropertyPath(property.Name)));
            }
        }

        return [.. names];
    }
}
