// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.given;

public class emitted_methods : Specification
{
    protected static IEventTypes Registry(params Type[] types)
    {
        var registry = Substitute.For<IEventTypes>();
        registry.AllClrTypes.Returns(types.ToImmutableList());
        registry.GetEventTypeFor(Arg.Any<Type>()).Returns(call => new EventType(call.Arg<Type>().Name, 1));
        return registry;
    }

    protected static Type Emit(Action<TypeBuilder> define)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        var model = assembly.DefineDynamicModule("SelfMethods").DefineType("State", TypeAttributes.Public);
        define(model);
        return model.CreateType();
    }

    protected static MethodBuilder Method(TypeBuilder model, Type eventType, bool context = false, MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Static)
    {
        var method = model.DefineMethod("Reduce", attributes, model, context ? [eventType, model, typeof(EventContext)] : [eventType, model]);
        method.GetILGenerator().Emit(OpCodes.Ldnull);
        method.GetILGenerator().Emit(OpCodes.Ret);
        return method;
    }

    public interface IRoot;
    public interface ILeft : IRoot;
    public interface IRight : IRoot;
    public record Joined : ILeft, IRight;
}
