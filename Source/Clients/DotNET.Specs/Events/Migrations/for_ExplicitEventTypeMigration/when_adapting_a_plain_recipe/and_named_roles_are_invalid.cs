// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Events.Migrations.for_ExplicitEventTypeMigration.when_adapting_a_plain_recipe;

public class and_named_roles_are_invalid : given.a_plain_migration
{
    Type[] _recipes;
    Exception[] _errors;

    void Establish() => _recipes = Enumerable.Range(0, 8).Select(EmitRecipe).ToArray();

    void Because() => _errors = _recipes.Select(_ => Catch.Exception(() => ExplicitMigrationMethods.For(_))).ToArray();

    [Fact] void should_reject_static_roles() => _errors[0].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_asynchronous_roles() => _errors[1].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_async_void_roles() => _errors[2].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_optional_parameters() => _errors[3].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_byref_parameters() => _errors[4].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_generic_roles() => _errors[5].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_overloaded_roles() => _errors[6].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_require_reversed_downcast_endpoints() => _errors[7].ShouldBeOfExactType<InvalidExplicitArtifact>();

    static Type EmitRecipe(int invalidShape)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("Recipes").DefineType("InvalidRecipe", TypeAttributes.Public);
        var upcastBuilder = typeof(IEventMigrationBuilder<Upgrade, Previous>);
        var flags = invalidShape == 0 ? MethodAttributes.Public | MethodAttributes.Static : MethodAttributes.Public;
        var upcast = type.DefineMethod(
            "Upcast",
            flags,
            invalidShape == 1 ? typeof(Task) : typeof(void),
            [invalidShape == 4 ? upcastBuilder.MakeByRefType() : upcastBuilder]);
        upcast.GetILGenerator().Emit(OpCodes.Ret);
        if (invalidShape == 2)
        {
            upcast.SetCustomAttribute(new CustomAttributeBuilder(typeof(AsyncStateMachineAttribute).GetConstructor([typeof(Type)])!, [typeof(object)]));
        }
        if (invalidShape == 3)
        {
            upcast.DefineParameter(1, ParameterAttributes.Optional, "builder");
        }
        if (invalidShape == 5)
        {
            upcast.DefineGenericParameters("T");
        }
        if (invalidShape == 6)
        {
            type.DefineMethod("Upcast", MethodAttributes.Public, typeof(void), [typeof(object)]).GetILGenerator().Emit(OpCodes.Ret);
        }
        type.DefineMethod(
            "Downcast",
            MethodAttributes.Public,
            typeof(void),
            [invalidShape == 7 ? upcastBuilder : typeof(IEventMigrationBuilder<Previous, Upgrade>)]).GetILGenerator().Emit(OpCodes.Ret);

        return type.CreateType();
    }
}
