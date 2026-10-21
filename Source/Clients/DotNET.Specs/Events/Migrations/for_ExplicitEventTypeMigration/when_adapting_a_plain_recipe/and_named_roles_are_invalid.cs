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

    void Establish() => _recipes = Enumerable.Range(0, 17).Select(EmitRecipe).ToArray();

    void Because() => _errors = _recipes.Select(_ => Catch.Exception(() => ExplicitMigrationMethods.For(_))).ToArray();

    [Fact] void should_reject_static_roles() => _errors[0].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_asynchronous_roles() => _errors[1].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_async_void_roles() => _errors[2].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_optional_parameters() => _errors[3].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_byref_parameters() => _errors[4].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_generic_roles() => _errors[5].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_overloaded_roles() => _errors[6].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_require_reversed_downcast_endpoints() => _errors[7].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_the_wrong_map_builder() => _errors[8].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_reversed_map_endpoints() => _errors[9].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_static_map_roles() => _errors[10].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_ambiguous_map_roles() => _errors[11].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_async_void_map_roles() => _errors[12].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_optional_map_parameters() => _errors[13].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_reject_hidden_inherited_roles() => _errors[14].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_require_the_upcast_role() => _errors[15].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_require_the_downcast_role() => _errors[16].ShouldBeOfExactType<InvalidExplicitArtifact>();

    internal static Type EmitRecipe(int invalidShape)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("Recipes").DefineType("InvalidRecipe", TypeAttributes.Public, invalidShape == 14 ? typeof(ValidBaseRecipe) : typeof(object));
        var upcastBuilder = typeof(IEventMigrationBuilder<Upgrade, Previous>);
        var flags = invalidShape == 0 ? MethodAttributes.Public | MethodAttributes.Static : MethodAttributes.Public;
        var upcast = type.DefineMethod(
            invalidShape == 15 ? "NotUpcast" : "Upcast",
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
            invalidShape == 16 ? "NotDowncast" : "Downcast",
            MethodAttributes.Public,
            typeof(void),
            [invalidShape == 7 ? upcastBuilder : typeof(IEventMigrationBuilder<Previous, Upgrade>)]).GetILGenerator().Emit(OpCodes.Ret);

        if (invalidShape is >= 8 and <= 13)
        {
            var mapType = invalidShape switch
            {
                8 => typeof(object),
                9 => typeof(IEventValueMapBuilder<Previous, Upgrade>),
                _ => typeof(IEventValueMapBuilder<Upgrade, Previous>)
            };
            var map = type.DefineMethod("MapValues", invalidShape == 10 ? MethodAttributes.Public | MethodAttributes.Static : MethodAttributes.Public, typeof(void), [mapType]);
            map.GetILGenerator().Emit(OpCodes.Ret);
            if (invalidShape == 11) type.DefineMethod("MapValues", MethodAttributes.Public, typeof(void), [typeof(object)]).GetILGenerator().Emit(OpCodes.Ret);
            if (invalidShape == 12) map.SetCustomAttribute(new CustomAttributeBuilder(typeof(AsyncStateMachineAttribute).GetConstructor([typeof(Type)])!, [typeof(object)]));
            if (invalidShape == 13) map.DefineParameter(1, ParameterAttributes.Optional, "builder");
        }

        return type.CreateType();
    }

    public class ValidBaseRecipe
    {
        public void Upcast(IEventMigrationBuilder<Upgrade, Previous> builder) { }
        public void Downcast(IEventMigrationBuilder<Previous, Upgrade> builder) { }
    }
}
