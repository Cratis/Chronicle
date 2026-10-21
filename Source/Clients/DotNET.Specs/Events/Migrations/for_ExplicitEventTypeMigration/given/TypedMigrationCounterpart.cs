// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace Cratis.Chronicle.Events.Migrations.for_ExplicitEventTypeMigration.given;

public static class TypedMigrationCounterpart
{
    public static Type For(Type recipe, Type upgrade, Type previous)
    {
        // Emit the base-inheriting counterpart so no test-only migration enters assembly discovery.
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        var baseType = typeof(EventTypeMigration<,>).MakeGenericType(upgrade, previous);
        var type = assembly.DefineDynamicModule("Counterparts").DefineType("TypedCounterpart", TypeAttributes.Public, baseType);
        var field = type.DefineField("_recipe", recipe, FieldAttributes.Private | FieldAttributes.InitOnly);
        var recipeConstructor = recipe.GetConstructors().Single();
        var dependencies = recipeConstructor.GetParameters().Select(_ => _.ParameterType).ToArray();
        var constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, [typeof(IEventTypes), .. dependencies]);
        var il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, baseType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(IEventTypes)], null)!);
        il.Emit(OpCodes.Ldarg_0);
        for (var index = 0; index < dependencies.Length; index++) il.Emit(OpCodes.Ldarg, index + 2);
        il.Emit(OpCodes.Newobj, recipeConstructor);
        il.Emit(OpCodes.Stfld, field);
        il.Emit(OpCodes.Ret);
        foreach (var name in new[] { "Upcast", "Downcast", "MapValues" })
        {
            var role = recipe.GetMethod(name);
            if (role is null) continue;
            var method = baseType.GetMethod(name)!;
            var implementation = type.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), [method.GetParameters()[0].ParameterType]);
            var body = implementation.GetILGenerator();
            body.Emit(OpCodes.Ldarg_0);
            body.Emit(OpCodes.Ldfld, field);
            body.Emit(OpCodes.Ldarg_1);
            body.Emit(OpCodes.Callvirt, role);
            body.Emit(OpCodes.Ret);
            type.DefineMethodOverride(implementation, method);
        }

        return type.CreateType();
    }
}
