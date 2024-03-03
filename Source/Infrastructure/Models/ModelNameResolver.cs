// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Reflection;

namespace Cratis.Models;

/// <summary>
/// Represents an implementation of <see cref="IModelNameConvention"/>.
/// </summary>
public class ModelNameResolver : IModelNameResolver
{
    readonly IModelNameConvention _convention;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelNameResolver"/> class.
    /// </summary>
    /// <param name="convention"><see cref="IModelNameConvention"/> to use.</param>
    public ModelNameResolver(IModelNameConvention convention)
    {
        _convention = convention;
    }

    /// <inheritdoc/>
    public string GetNameFor(Type readModelType)
    {
        if (readModelType.HasAttribute<ModelNameAttribute>())
        {
            return readModelType.GetCustomAttribute<ModelNameAttribute>(false)!.Name;
        }

        return _convention.GetNameFor(readModelType);
    }
}
