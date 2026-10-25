// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Security.given;

public static class store_scoped_query_inventory
{
    public static IEnumerable<string> For(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.GetParameters().Any(parameter => string.Equals(parameter.Name, "eventStore", StringComparison.OrdinalIgnoreCase)))
            .Select(Identity);

    public static string Identity(MethodInfo method) =>
        $"{method.DeclaringType!.FullName}.{method.Name}({string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.ToString()))})";
}
