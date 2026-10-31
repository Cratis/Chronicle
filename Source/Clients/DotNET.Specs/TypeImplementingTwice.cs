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
    /// Builds a type that declares both closed interfaces itself.
    /// </summary>
    /// <param name="openInterface">The open generic artifact interface, for example <see cref="Projections.IProjectionFor{TReadModel}"/>.</param>
    /// <param name="first">The first read model type.</param>
    /// <param name="second">The second read model type.</param>
    /// <returns>The built type.</returns>
    public static TypeInfo Of(Type openInterface, Type first, Type second)
    {
        var module = DefineModule();
        var type = module.DefineType($"TypeImplementingTwice{openInterface.Name.Split('`')[0]}", TypeAttributes.Public);
        Implement(type, openInterface.MakeGenericType(first));
        Implement(type, openInterface.MakeGenericType(second));
        return type.CreateTypeInfo();
    }

    /// <summary>
    /// Builds a type that inherits the first closed interface from its base class and declares the second itself.
    /// </summary>
    /// <param name="openInterface">The open generic artifact interface, for example <see cref="Projections.IProjectionFor{TReadModel}"/>.</param>
    /// <param name="first">The read model type of the interface the base class implements.</param>
    /// <param name="second">The read model type of the interface the derived class implements.</param>
    /// <returns>The built derived type.</returns>
    public static TypeInfo ThroughInheritance(Type openInterface, Type first, Type second)
    {
        var module = DefineModule();
        var name = openInterface.Name.Split('`')[0];
        var baseType = module.DefineType($"BaseImplementing{name}", TypeAttributes.Public);
        Implement(baseType, openInterface.MakeGenericType(first));
        var derived = module.DefineType($"DerivedImplementing{name}", TypeAttributes.Public, baseType.CreateTypeInfo());
        Implement(derived, openInterface.MakeGenericType(second));
        return derived.CreateTypeInfo();
    }

    static ModuleBuilder DefineModule() =>
        AssemblyBuilder
            .DefineDynamicAssembly(new AssemblyName($"ArtifactFixtures_{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect)
            .DefineDynamicModule("ArtifactFixtures");

    static void Implement(TypeBuilder type, Type closedInterface)
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
}
