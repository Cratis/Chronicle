// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using ProtoBuf;

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

/// <summary>
/// These attributes are reflection inputs, not declarations for the protobuf AOT generator.
/// </summary>
internal static class a_sample_wire_message
{
    internal static Type Contract { get; } = Create();

    static Type Create()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ApiBaselineSamples"), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("Samples").DefineType("ApiBaselineSamples.Message", TypeAttributes.Public);
        type.SetCustomAttribute(new CustomAttributeBuilder(typeof(ProtoContractAttribute).GetConstructor(Type.EmptyTypes)!, []));
        var property = type.DefineProperty("Names", PropertyAttributes.None, typeof(string), Type.EmptyTypes);
        var getter = type.DefineMethod("get_Names", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(string), Type.EmptyTypes);
        var body = getter.GetILGenerator();
        body.Emit(OpCodes.Ldnull);
        body.Emit(OpCodes.Ret);
        property.SetGetMethod(getter);
        property.SetCustomAttribute(new CustomAttributeBuilder(
            typeof(ProtoMemberAttribute).GetConstructor([typeof(int)])!,
            [3],
            [typeof(ProtoMemberAttribute).GetProperty(nameof(ProtoMemberAttribute.IsRequired))!],
            [false]));
        return type.CreateType()!;
    }
}
