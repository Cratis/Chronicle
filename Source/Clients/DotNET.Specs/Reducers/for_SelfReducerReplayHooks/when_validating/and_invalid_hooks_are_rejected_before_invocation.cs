// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers.for_SelfReducerInvoker.given;

namespace Cratis.Chronicle.Reducers.for_SelfReducerReplayHooks.when_validating;

public class and_invalid_hooks_are_rejected_before_invocation : emitted_methods
{
    Exception[] _failures;

    void Because() => _failures = Enumerable.Range(0, 18).Select(shape => Catch.Exception(() =>
    {
        var model = Emit(type =>
        {
            Method(type, typeof(Joined));
            var name = "BeginReplay";
            var attributes = MethodAttributes.Public | MethodAttributes.Static;
            var result = typeof(Task);
            Type[] parameters = [];
            switch (shape)
            {
                case 0: attributes = MethodAttributes.Private | MethodAttributes.Static; break;
                case 1: attributes = MethodAttributes.Public; break;
                case 3: result = typeof(void); break;
                case 4: result = typeof(Task<int>); break;
                case 5: result = typeof(ValueTask); break;
                case 6: parameters = [typeof(IServiceProvider).MakeByRefType()]; break;
                case 7:
                case 8: parameters = [typeof(IServiceProvider)]; break;
                case 9: name = "BeginReplayPartition"; break;
                case 10: name = "EndReplayPartition"; parameters = [typeof(string)]; break;
                case 11: parameters = [typeof(Partition)]; break;
                case 12: parameters = [typeof(EventContext)]; break;
                case 13: parameters = [type]; break;
                case 14: parameters = [typeof(Joined)]; break;
                case 15: parameters = [typeof(IRoot)]; break;
                case 16: parameters = [typeof(int)]; break;
                case 17:
                    var parent = ((ModuleBuilder)type.Module).DefineType("Parent", TypeAttributes.Public);
                    var inherited = parent.DefineMethod(name, attributes, result, Type.EmptyTypes);
                    inherited.GetILGenerator().Emit(OpCodes.Ldnull);
                    inherited.GetILGenerator().Emit(OpCodes.Ret);
                    type.SetParent(parent.CreateType());
                    break;
            }
            var hook = type.DefineMethod(name, attributes, result, parameters);
            if (shape == 2) hook.DefineGenericParameters("T");
            if (shape == 7) hook.DefineParameter(1, ParameterAttributes.Optional, "service");
            if (shape == 8) hook.DefineParameter(1, ParameterAttributes.None, "service").SetCustomAttribute(new CustomAttributeBuilder(typeof(ParamArrayAttribute).GetConstructor(Type.EmptyTypes)!, []));
            hook.GetILGenerator().Emit(OpCodes.Ldnull);
            hook.GetILGenerator().Emit(OpCodes.Ret);
        });
        _ = new SelfReducerInvoker(Registry(typeof(Joined)), model, "state");
    })).ToArray();

    [Fact] void should_validate_every_invalid_shape_before_model_or_service_activation() => _failures.All(failure => failure is InvalidSelfReducerLifecycleHook).ShouldBeTrue();
}
