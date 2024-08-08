// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace Cratis.Chronicle;

/// <summary>
/// Helper methods for substitutes.
/// </summary>
public static class Substitutes
{
    static readonly AssemblyBuilder _assemblyBuilder;
    static readonly ModuleBuilder _moduleBuilder;

    static Substitutes()
    {
        var assemblyName = new AssemblyName("DynamicAssemblyForDynamicTypes");
        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        _moduleBuilder = _assemblyBuilder.DefineDynamicModule("MainModule");
    }

    /// <summary>
    /// Create a unique type of a type.
    /// </summary>
    /// <typeparam name="T">Type to create substitute for.</typeparam>
    /// <returns>Unique type.</returns>
    public static Type CreateUniqueTypeOf<T>()
    {
        var typeBuilder = _moduleBuilder.DefineType(
            $"{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Interface,
            null,
            [typeof(T)]);

        return typeBuilder.CreateType()!;
    }

    /// <summary>
    /// Create a unique substitute for a type.
    /// </summary>
    /// <typeparam name="T">Type to create substitute for.</typeparam>
    /// <returns>Unique type.</returns>
    public static T UniqueFor<T>()
    {
        var type = CreateUniqueTypeOf<T>();

        return (T)SubstitutionContext.Current.SubstituteFactory.Create([type], []);
    }
}
