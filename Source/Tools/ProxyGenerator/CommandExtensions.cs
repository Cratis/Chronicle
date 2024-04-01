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
    /// <returns>Converted <see cref="CommandDescriptor"/>.</returns>
    public static CommandDescriptor ToCommandDescriptor(this MethodInfo method)
    {
        var imports = new List<ImportStatement>();
        var typesInvolved = new List<Type>();
        var properties = method.GetPropertyDescriptors();
        var hasResponse = false;
        var responseModel = ModelDescriptor.Empty;

        if (method.ReturnType.IsAssignableTo(typeof(Task)) && method.ReturnType.IsGenericType)
        {
            hasResponse = true;
            var responseType = method.ReturnType.GetGenericArguments()[0];
            responseModel = responseType.ToModelDescriptor();
            typesInvolved.Add(responseType);
        }
        else if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task))
        {
            hasResponse = true;
            responseModel = method.ReturnType.ToModelDescriptor();
            typesInvolved.Add(method.ReturnType);
        }

        var commandPath = method.DeclaringType!.ResolveTargetPath();
        typesInvolved.AddRange(properties.Select(_ => _.OriginalType).Where(t_ => !t_.IsKnownType()));
        imports.AddRange(typesInvolved.Select(_ =>
        {
            var importPath = Path.GetRelativePath(commandPath, _.ResolveTargetPath());
            importPath = $"{importPath}/{_.Name}";
            return new ImportStatement(_.GetTargetType().Type, importPath);
        }));

        return new CommandDescriptor(
            method.DeclaringType!,
            method,
            method.GetRoute(),
            method.Name,
            properties,
            imports,
            method.GetArgumentDescriptors(),
            hasResponse,
            responseModel,
            typesInvolved);
    }

    /// <summary>
    /// Get properties from a <see cref="MethodInfo"/>.
    /// </summary>
    /// <param name="method">Method to get for.</param>
    /// <returns>Collection of <see cref="PropertyDescriptor"/>.</returns>
    public static IEnumerable<PropertyDescriptor> GetPropertyDescriptors(this MethodInfo method)
    {
        return method.GetParameters().ToList().ConvertAll(_ => _.ToPropertyDescriptor());
    }
}
