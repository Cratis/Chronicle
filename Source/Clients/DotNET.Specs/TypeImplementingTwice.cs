// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace Cratis.Chronicle;

/// <summary>
/// Builds, at runtime, a type that implements two closed versions of an open generic artifact interface.
/// </summary>
/// <remarks>
/// The type lives in its own dynamic assembly. Declared in source, it would sit in the specification assembly,
/// and every specification that discovers artifacts by scanning that assembly would fail on it.
/// Only interfaces whose methods all return <see langword="void"/> are supported.
/// </remarks>
public static class TypeImplementingTwice
{
    /// <summary>
    /// Builds the type.
    /// </summary>
    /// <param name="openInterface">The open generic artifact interface, for example <see cref="Projections.IProjectionFor{TReadModel}"/>.</param>
    /// <param name="first">The first read model type.</param>
    /// <param name="second">The second read model type.</param>
    /// <returns>The built type.</returns>
    public static TypeInfo Of(Type openInterface, Type first, Type second)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"ArtifactFixtures_{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect);
        var type = assembly.DefineDynamicModule("ArtifactFixtures").DefineType($"TypeImplementingTwice{openInterface.Name.Split('`')[0]}", TypeAttributes.Public);
        foreach (var closedInterface in new[] { openInterface.MakeGenericType(first), openInterface.MakeGenericType(second) })
        {
            type.AddInterfaceImplementation(closedInterface);
            foreach (var method in closedInterface.GetMethods())
            {
                var implementation = type.DefineMethod(
                    $"{closedInterface}.{method.Name}",
                    MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                    method.ReturnType,
                    method.GetParameters().Select(_ => _.ParameterType).ToArray());
                implementation.GetILGenerator().Emit(OpCodes.Ret);
                type.DefineMethodOverride(implementation, method);
            }
        }

        return type.CreateTypeInfo();
    }
}
