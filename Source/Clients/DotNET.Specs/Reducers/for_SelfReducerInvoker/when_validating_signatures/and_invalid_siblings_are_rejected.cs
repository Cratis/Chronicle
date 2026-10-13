// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_validating_signatures;

public class and_invalid_siblings_are_rejected : given.emitted_methods
{
    Exception[] _failures;

    void Because() => _failures = Enumerable.Range(0, 18).Select(shape => Catch.Exception(() =>
    {
        var model = Emit(type =>
        {
            Method(type, typeof(Joined));
            var parameters = new Type[] { typeof(IRoot), type };
            var attributes = MethodAttributes.Public | MethodAttributes.Static;
            Type result = type;
            switch (shape)
            {
                case 0: attributes = MethodAttributes.Private | MethodAttributes.Static; break;
                case 1: attributes = MethodAttributes.Public; break;
                case 2: parameters = [typeof(IRoot)]; break;
                case 3: parameters = [typeof(IRoot), type, typeof(EventContext), typeof(IServiceProvider)]; break;
                case 4: parameters = [typeof(IRoot).MakeByRefType(), type]; break;
                case 5: parameters = [typeof(IRoot), type.MakeByRefType()]; break;
                case 6: parameters = [typeof(object), type]; break;
                case 7: parameters = [typeof(int), type]; break;
                case 8: parameters = [typeof(IRoot), typeof(object)]; break;
                case 9: parameters = [typeof(IRoot), type, typeof(IServiceProvider)]; break;
                case 10: result = typeof(Task); break;
                case 11: result = typeof(Task<object>); break;
                case 12: result = typeof(ValueTask<>).MakeGenericType(type); break;
                case 15: result = typeof(void); break;
            }
            var method = type.DefineMethod("Reduce", attributes, result, parameters);
            if (shape == 13) method.DefineGenericParameters("T");
            if (shape == 14) method.DefineParameter(2, ParameterAttributes.Optional, "current");
            if (shape == 16)
            {
                var nullable = typeof(object).Assembly.GetType("System.Runtime.CompilerServices.NullableAttribute")!;
                method.DefineParameter(2, ParameterAttributes.None, "current").SetCustomAttribute(new CustomAttributeBuilder(nullable.GetConstructor([typeof(byte)])!, [(byte)1]));
            }
            if (shape == 17) method.DefineParameter(1, ParameterAttributes.None, "event").SetCustomAttribute(new CustomAttributeBuilder(typeof(ParamArrayAttribute).GetConstructor(Type.EmptyTypes)!, []));
            method.GetILGenerator().Emit(OpCodes.Ldnull);
            method.GetILGenerator().Emit(OpCodes.Ret);
        });
        _ = new SelfReducerInvoker(Registry(typeof(Joined)), model, "state");
    })).ToArray();

    [Fact] void should_reject_every_invalid_sibling_after_structural_detection() => _failures.All(failure => failure is InvalidSelfReducerSignature).ShouldBeTrue();
}
