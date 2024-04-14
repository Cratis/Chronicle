// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.ProxyGenerator.Templates;

namespace Cratis.ProxyGenerator;

/// <summary>
/// Extension methods for working with commands.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Convert a <see cref="MethodInfo"/> to a <see cref="CommandDescriptor"/>.
    /// </summary>
    /// <param name="method">Method to convert.</param>
    /// <returns>Converted <see cref="CommandDescriptor"/>.</returns>
    public static QueryDescriptor ToQueryDescriptor(this MethodInfo method)
    {
        var typesInvolved = new List<Type>();
        var properties = method.GetPropertyDescriptors();
        var responseModel = ModelDescriptor.Empty;

        if (method.ReturnType.IsAssignableTo(typeof(Task)) && method.ReturnType.IsGenericType)
        {
            var responseType = method.ReturnType.GetGenericArguments()[0];
            responseModel = responseType.ToModelDescriptor();
            typesInvolved.Add(responseType);
        }
        else if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task))
        {
            responseModel = method.ReturnType.ToModelDescriptor();
            typesInvolved.Add(method.ReturnType);
        }

        var propertiesWithComplexTypes = properties.Where(_ => !_.OriginalType.IsKnownType());
        typesInvolved.AddRange(propertiesWithComplexTypes.Select(_ => _.OriginalType));
        var imports = typesInvolved.GetImports(method.DeclaringType!.ResolveTargetPath());

        typesInvolved = [];
        foreach (var property in propertiesWithComplexTypes)
        {
            property.CollectTypesInvolved(typesInvolved);
        }

        return new(
            method.DeclaringType!,
            method,
            method.GetRoute(),
            method.Name,
            "Model", // TODO: Get the model type from the method
            "Constructor", // TODO: Get the constructor from the method
            false, // TODO: Whether or not it is an enumerable
            imports,
            method.GetArgumentDescriptors());
    }
}
