// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Strings;
using Humanizer;

namespace Cratis.Models;

/// <summary>
/// Represents an implementation of <see cref="IModelNameConvention"/> for the default convention.
/// </summary>
/// <remarks>
/// The default convention is to pluralize the type name and convert it to camel case.
/// </remarks>
public class DefaultModelNameConvention : IModelNameConvention
{
    /// <inheritdoc/>
    public string GetNameFor(Type type)
    {
        var modelName = type.Name.Pluralize();
        return modelName.ToCamelCase();
    }
}
