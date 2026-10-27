// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

internal static class public_api_snapshot
{
    const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    internal static string Render(IEnumerable<Type> types)
    {
        var lines = types.SelectMany(Declarations).Order(StringComparer.Ordinal).ToArray();
        if (lines.Length == 0 || lines.Distinct(StringComparer.Ordinal).Count() != lines.Length)
        {
            throw new InvalidOperationException("A public API snapshot must contain nonduplicate declarations.");
        }

        return string.Join('\n', lines) + "\n";
    }

    static IEnumerable<string> Declarations(Type type)
    {
        var owner = public_api_type_name.Render(type);
        return new[] { TypeDeclaration(type, owner) }
            .Concat(public_api_attributes.Render(owner, type.CustomAttributes))
            .Concat(public_api_signature.Constraints(owner, type.GetGenericArguments()))
            .Concat(Members(type).SelectMany(MemberDeclarations));
    }

    static string TypeDeclaration(Type type, string owner)
    {
        var kind = Kind(type);
        var visibility = type switch
        {
            { IsNestedPublic: true } => "nested public",
            { IsPublic: true } => "public",
            _ => "nonpublic"
        };
        return $"type {owner} {visibility} {kind} arity={type.GetGenericArguments().Length} abstract={type.IsAbstract} sealed={type.IsSealed} base={Base(type)} interfaces=[{string.Join(',', type.GetInterfaces().Select(RenderType).Order(StringComparer.Ordinal))}]";
    }

    static IEnumerable<MemberInfo> Members(Type type)
    {
        var accessors = type.GetProperties(Declared).SelectMany(property => property.GetAccessors(true))
            .Concat(type.GetEvents(Declared).SelectMany(Accessors)).ToHashSet();
        return type.GetMembers(Declared).Where(member => IsDeclaredMember(member, accessors));
    }

    static bool IsDeclaredMember(MemberInfo member, HashSet<MethodInfo> accessors) => member switch
    {
        Type => false,
        MethodInfo method => !accessors.Contains(method),
        _ => true
    };

    static IEnumerable<string> MemberDeclarations(MemberInfo member)
    {
        var declaration = Member(member);
        return new[] { declaration }
            .Concat(public_api_attributes.Render(declaration, member.CustomAttributes))
            .Concat(MethodDetails(declaration, member))
            .Concat(MemberAccessors(member).SelectMany(accessor => AccessorDeclarations(declaration, accessor)));
    }

    static IEnumerable<string> AccessorDeclarations(string declaration, MethodInfo accessor)
    {
        var key = $"accessor {declaration} {public_api_signature.Method(accessor)}";
        return new[] { key }
            .Concat(public_api_attributes.Render(key, accessor.CustomAttributes))
            .Concat(MethodDetails(key, accessor));
    }

    static string Kind(Type type) => type switch
    {
        { IsConstructedGenericType: true } => throw new NotSupportedException("Snapshot generic type definitions, not constructed generic types."),
        { IsInterface: true } => "interface",
        { IsEnum: true } => "enum",
        { IsValueType: true } => "struct",
        _ => typeof(MulticastDelegate).IsAssignableFrom(type) ? "delegate" : "class"
    };

    static string Member(MemberInfo member) => member switch
    {
        MethodBase method => $"method {public_api_signature.Method(method)}",
        PropertyInfo property => Property(property),
        FieldInfo field => $"field {Owner(field)} public{(field.IsStatic ? " static" : string.Empty)}{(field.IsInitOnly ? " readonly" : string.Empty)}{(field.IsLiteral ? " const" : string.Empty)} {public_api_type_name.Render(field.FieldType, new NullabilityInfoContext().Create(field))}{public_api_signature.Modifiers(field.GetRequiredCustomModifiers(), field.GetOptionalCustomModifiers())}{(field.IsLiteral ? $" = {public_api_attributes.Value(field.GetRawConstantValue())}" : string.Empty)}",
        EventInfo @event => $"event {Owner(@event)} {RenderType(@event.EventHandlerType!)}",
        _ => throw new NotSupportedException($"Unsupported public member {member.DeclaringType?.FullName}::{member.Name}: {member.MemberType}")
    };

    static string Property(PropertyInfo property)
    {
        var nullable = new NullabilityInfoContext().Create(property);
        return $"property {Owner(property)}({string.Join(',', property.GetIndexParameters().Select(public_api_signature.Parameter))}) {public_api_type_name.Render(property.PropertyType, nullable)} write={nullable.WriteState}{public_api_signature.Modifiers(property.GetRequiredCustomModifiers(), property.GetOptionalCustomModifiers())}";
    }

    static IEnumerable<string> MethodDetails(string owner, MemberInfo member) => member switch
    {
        MethodBase method => method.GetParameters()
            .SelectMany(parameter => public_api_attributes.Render($"{owner} parameter[{parameter.Position}]", parameter.CustomAttributes))
            .Concat(ReturnDetails(owner, method)),
        _ => []
    };

    static IEnumerable<string> ReturnDetails(string owner, MethodBase method) => method switch
    {
        MethodInfo info => public_api_attributes.Render($"{owner} return", info.ReturnParameter.CustomAttributes)
            .Concat(public_api_signature.Constraints(owner, info.GetGenericArguments())),
        _ => []
    };

    static IEnumerable<MethodInfo> MemberAccessors(MemberInfo member) => member switch
    {
        PropertyInfo property => property.GetAccessors(true),
        EventInfo @event => Accessors(@event),
        _ => []
    };

    static IEnumerable<MethodInfo> Accessors(EventInfo @event) => new[] { @event.GetAddMethod(true), @event.GetRemoveMethod(true), @event.GetRaiseMethod(true) }.OfType<MethodInfo>();

    static string Base(Type type)
    {
        if (type.IsEnum)
        {
            return RenderType(Enum.GetUnderlyingType(type));
        }

        return type.BaseType is { } parent ? RenderType(parent) : "none";
    }

    static string Owner(MemberInfo member) => $"{RenderType(member.DeclaringType!)}::{member.Name}";

    static string RenderType(Type type) => public_api_type_name.Render(type);
}
