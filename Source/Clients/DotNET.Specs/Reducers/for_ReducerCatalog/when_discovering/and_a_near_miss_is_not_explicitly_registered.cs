// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_discovering;

public class and_a_near_miss_is_not_explicitly_registered : given.self_reducers
{
    Type _nearMiss;
    Exception _explicitError;

    void Establish()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("NearMiss").DefineType("NearMissModel", TypeAttributes.Public);
        var method = type.DefineMethod("Reduce", MethodAttributes.Public | MethodAttributes.Static, type, [typeof(OwnershipEvent), type, typeof(string)]);
        method.GetILGenerator().Emit(OpCodes.Ldnull);
        method.GetILGenerator().Emit(OpCodes.Ret);
        _nearMiss = type.CreateType();
        _artifacts.Reducers.Returns([_nearMiss]);
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _explicitError = await Catch.Exception(() => _store.Reducers.Register(_nearMiss));
    }

    [Fact] void should_leave_discovery_nonfatal_and_unregistered() => _store.Reducers.GetAllHandlers().ShouldBeEmpty();
    [Fact] void should_refuse_the_same_signature_on_explicit_registration() => _explicitError.ShouldBeOfExactType<InvalidSelfReducerSignature>();
    [Fact] void should_not_recognize_a_supported_shape() => SelfReducerMethods.HasSelfShape(_nearMiss).ShouldBeFalse();
}
