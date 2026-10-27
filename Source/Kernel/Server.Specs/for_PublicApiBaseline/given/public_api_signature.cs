// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

internal static class public_api_signature
{
    internal static string Parameter(ParameterInfo parameter)
    {
        var nullable = new NullabilityInfoContext().Create(parameter);
        var direction = Direction(parameter);
        var optional = parameter.IsOptional ? $" optional={public_api_attributes.Value(parameter.DefaultValue)}" : string.Empty;
        var variadic = parameter.IsDefined(typeof(ParamArrayAttribute)) ? "params " : string.Empty;
        return $"{variadic}{direction}{public_api_type_name.Render(parameter.ParameterType, nullable)} {parameter.Name}{optional}{Modifiers(parameter.GetRequiredCustomModifiers(), parameter.GetOptionalCustomModifiers())}";
    }

    internal static string Method(MethodBase method)
    {
        var result = method is MethodInfo info
            ? public_api_type_name.Render(info.ReturnType, new NullabilityInfoContext().Create(info.ReturnParameter)) + Modifiers(info.ReturnParameter.GetRequiredCustomModifiers(), info.ReturnParameter.GetOptionalCustomModifiers())
            : "constructor";
        var generic = method.IsGenericMethod ? $"<{string.Join(',', method.GetGenericArguments().Select(RenderType))}>" : string.Empty;
        return $"{Flags(method)} {result} {public_api_type_name.Render(method.DeclaringType!)}::{method.Name}{generic}({string.Join(',', method.GetParameters().Select(Parameter))}) calling={method.CallingConvention}";
    }

    internal static string Flags(MethodBase method)
    {
        var flags = $"{Visibility(method)}{(method.IsStatic ? " static" : string.Empty)}{(method.IsAbstract ? " abstract" : string.Empty)}{(method.IsVirtual ? " virtual" : string.Empty)}{(method.IsFinal ? " final" : string.Empty)}";
        var overridden = method is MethodInfo candidate && candidate.GetBaseDefinition() != candidate ? " override" : string.Empty;
        return flags + overridden;
    }

    internal static string Visibility(MethodBase method) => method switch
    {
        { IsPublic: true } => "public",
        { IsFamily: true } => "protected",
        { IsFamilyOrAssembly: true } => "protected internal",
        { IsFamilyAndAssembly: true } => "private protected",
        { IsAssembly: true } => "internal",
        _ => "private"
    };

    internal static string Modifiers(Type[] required, Type[] optional) =>
        string.Concat(required.Select(type => $" modreq({public_api_type_name.Render(type)})")) + string.Concat(optional.Select(type => $" modopt({public_api_type_name.Render(type)})"));

    internal static IEnumerable<string> Constraints(string owner, IEnumerable<Type> parameters) => parameters.Select(parameter =>
        $"generic {owner} {public_api_type_name.Render(parameter)} flags={parameter.GenericParameterAttributes} constraints=[{string.Join(',', parameter.GetGenericParameterConstraints().Select(RenderType).Order(StringComparer.Ordinal))}] attributes=[{string.Join(';', public_api_attributes.Render("parameter", parameter.CustomAttributes).Order(StringComparer.Ordinal))}]");

    static string Direction(ParameterInfo parameter)
    {
        if (!parameter.ParameterType.IsByRef)
        {
            return string.Empty;
        }

        return parameter switch
        {
            { IsOut: true } => "out ",
            { IsIn: true } => "in ",
            _ => "ref "
        };
    }

    static string RenderType(Type type) => public_api_type_name.Render(type);
}
