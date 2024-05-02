// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.ProxyGenerator.Templates;

namespace Cratis.ProxyGenerator;

/// <summary>
/// Extension methods for working with commands.
/// </summary>
public static class CommandExtensions
{
    /// <summary>
    /// Convert a <see cref="MethodInfo"/> to a <see cref="CommandDescriptor"/>.
    /// </summary>
    /// <param name="method">Method to convert.</param>
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <returns>Converted <see cref="CommandDescriptor"/>.</returns>
    public static CommandDescriptor ToCommandDescriptor(this MethodInfo method, string targetPath)
    {
        var typesInvolved = new List<Type>();
        var properties = method.GetPropertyDescriptors();
        var hasResponse = false;
        var responseModel = ModelDescriptor.Empty;

        if (method.ReturnType.IsAssignableTo(typeof(Task)) && method.ReturnType.IsGenericType)
        {
            hasResponse = true;
            var responseType = method.ReturnType.GetGenericArguments()[0];
            responseModel = responseType.ToModelDescriptor();
        }
        else if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task))
        {
            hasResponse = true;
            responseModel = method.ReturnType.ToModelDescriptor();
        }

        if (!(responseModel.Type?.IsKnownType() ?? true))
        {
            typesInvolved.Add(responseModel.Type);
        }

        var propertiesWithComplexTypes = properties.Where(_ => !_.OriginalType.IsKnownType());
        typesInvolved.AddRange(propertiesWithComplexTypes.Select(_ => _.OriginalType));
        var imports = typesInvolved.GetImports(targetPath, method.DeclaringType!.ResolveTargetPath());

        var additionalTypesInvolved = new List<Type>();
        foreach (var property in propertiesWithComplexTypes)
        {
            property.CollectTypesInvolved(additionalTypesInvolved);
        }

        return new(
            method.DeclaringType!,
            method,
            method.GetRoute(),
            method.Name,
            properties,
            imports,
            method.GetArgumentDescriptors(),
            hasResponse,
            responseModel,
            [.. typesInvolved, .. additionalTypesInvolved]);
    }
}
