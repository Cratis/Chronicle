// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

internal static class public_api_accessor
{
    internal static string Render(MemberInfo member, MethodInfo accessor)
    {
        var owner = $"{public_api_type_name.Render(member.DeclaringType!)}::{member.Name}";
        return $"accessor {owner} {Kind(member, accessor)} {public_api_signature.Flags(accessor)}{Modifiers(member, accessor)} calling={accessor.CallingConvention}";
    }

    static string Kind(MemberInfo member, MethodInfo accessor) => member switch
    {
        PropertyInfo property => PropertyKind(property, accessor),
        EventInfo @event => EventKind(@event, accessor),
        _ => throw new NotSupportedException($"Unsupported accessor owner: {member.MemberType}")
    };

    static string PropertyKind(PropertyInfo property, MethodInfo accessor)
    {
        if (accessor == property.GetMethod)
        {
            return "get";
        }

        return accessor.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit)) ? "init" : "set";
    }

    static string EventKind(EventInfo @event, MethodInfo accessor)
    {
        if (accessor == @event.AddMethod)
        {
            return "add";
        }

        return accessor == @event.RemoveMethod ? "remove" : "raise";
    }

    static string Modifiers(MemberInfo member, MethodInfo accessor)
    {
        var propertyRequired = member is PropertyInfo property ? property.GetRequiredCustomModifiers() : Type.EmptyTypes;
        var propertyOptional = member is PropertyInfo info ? info.GetOptionalCustomModifiers() : Type.EmptyTypes;
        var required = accessor.ReturnParameter.GetRequiredCustomModifiers().Except(propertyRequired.Append(typeof(IsExternalInit))).ToArray();
        var optional = accessor.ReturnParameter.GetOptionalCustomModifiers().Except(propertyOptional).ToArray();
        return public_api_signature.Modifiers(required, optional);
    }
}
