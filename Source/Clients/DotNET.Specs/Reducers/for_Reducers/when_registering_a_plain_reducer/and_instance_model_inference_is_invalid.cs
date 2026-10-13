// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_a_plain_reducer;

public class and_instance_model_inference_is_invalid : an_explicit_store
{
    Type _staticOnly;
    Type _ambiguous;
    Exception _staticError;
    Exception _ambiguousError;

    async Task Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        _store = CreateStore();
        await _store.DiscoverAll();
        _staticOnly = EmitReducer(staticOnly: true, ambiguous: false);
        _ambiguous = EmitReducer(staticOnly: false, ambiguous: true);
    }

    async Task Because()
    {
        _staticError = await Catch.Exception(() => _store.Reducers.Register(_staticOnly));
        _ambiguousError = await Catch.Exception(() => _store.Reducers.Register(_ambiguous));
    }

    [Fact] void should_not_treat_static_self_reduce_as_an_instance_reducer() => _staticError.ShouldBeOfExactType<CannotInferReducerReadModel>();
    [Fact] void should_refuse_multiple_supported_read_models() => _ambiguousError.ShouldBeOfExactType<CannotInferReducerReadModel>();

    static Type EmitReducer(bool staticOnly, bool ambiguous)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("Reducers").DefineType("InvalidReducer", TypeAttributes.Public);
        var attributes = staticOnly ? MethodAttributes.Public | MethodAttributes.Static : MethodAttributes.Public;
        var first = type.DefineMethod("Reduce", attributes, typeof(Model), [typeof(PlainEvent), typeof(Model)]);
        first.GetILGenerator().Emit(OpCodes.Ldnull);
        first.GetILGenerator().Emit(OpCodes.Ret);
        if (ambiguous)
        {
            var second = type.DefineMethod("ReduceOther", MethodAttributes.Public, typeof(OtherModel), [typeof(PlainEvent), typeof(OtherModel)]);
            second.GetILGenerator().Emit(OpCodes.Ldnull);
            second.GetILGenerator().Emit(OpCodes.Ret);
        }

        return type.CreateType();
    }

    public record Model(string Name);

    public record OtherModel(string Name);
}
