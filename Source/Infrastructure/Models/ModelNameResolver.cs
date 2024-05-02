// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Reflection;

namespace Cratis.Models;

/// <summary>
/// Represents an implementation of <see cref="IModelNameConvention"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ModelNameResolver"/> class.
/// </remarks>
/// <param name="convention"><see cref="IModelNameConvention"/> to use.</param>
public class ModelNameResolver(IModelNameConvention convention) : IModelNameResolver
{
    /// <inheritdoc/>
    public string GetNameFor(Type readModelType)
    {
        if (readModelType.HasAttribute<ModelNameAttribute>())
        {
            return readModelType.GetCustomAttribute<ModelNameAttribute>(false)!.Name;
        }

        return convention.GetNameFor(readModelType);
    }
}
