// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Events.Migrations.for_ExplicitEventTypeMigration.given;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigrators.when_binding_to_a_store.given;

public class store_bindings : a_plain_migration
{
    protected IClientArtifactsProvider _discovered;
    protected IClientArtifactsProvider _catalog;
    protected IEventTypeMigrators _bound;

    void Establish()
    {
        _discovered = Substitute.For<IClientArtifactsProvider>();
        _eventTypes.HasFor(typeof(Previous)).Returns(true);
        _eventTypes.HasFor(typeof(Upgrade)).Returns(true);
        _catalog = new ExplicitArtifacts()
            .RegisterEventType<Previous>(_ => _.WithId("shared"))
            .RegisterEventType<Upgrade>(_ => _.WithId("shared").Generation(2))
            .RegisterMigration<PlainMigration>()
            .RegisterMigration<PlainMigration>()
            .CreateSnapshot().ApplyTo(_discovered);
    }

    protected static Type EmitTypedMigration(Type upgrade, Type previous, bool registryPath, bool requestsRegistry = true)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        var baseType = typeof(EventTypeMigration<,>).MakeGenericType(upgrade, previous);
        var type = assembly.DefineDynamicModule("Migrations").DefineType("TypedRecipe", TypeAttributes.Public, baseType);
        var constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, requestsRegistry ? [typeof(IEventTypes)] : []);
        var il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        if (registryPath) il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, baseType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, registryPath ? [typeof(IEventTypes)] : [], null)!);
        il.Emit(OpCodes.Ret);
        foreach (var name in new[] { "Upcast", "Downcast" })
        {
            var method = baseType.GetMethod(name)!;
            var implementation = type.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), [method.GetParameters()[0].ParameterType]);
            implementation.GetILGenerator().Emit(OpCodes.Ret);
            type.DefineMethodOverride(implementation, method);
        }

        return type.CreateType();
    }
}
