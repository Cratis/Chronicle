// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

internal static class public_api_visibility
{
    internal static bool Included(MethodBase method) => method switch
    {
        { IsPublic: true } => true,
        { IsFamily: true } => IsExtensible(method.DeclaringType),
        { IsFamilyOrAssembly: true } => IsExtensible(method.DeclaringType),
        _ => false
    };

    internal static bool Included(FieldInfo field) => field switch
    {
        { IsPublic: true } => true,
        { IsFamily: true } => IsExtensible(field.DeclaringType),
        { IsFamilyOrAssembly: true } => IsExtensible(field.DeclaringType),
        _ => false
    };

    internal static string Visibility(FieldInfo field) => field switch
    {
        { IsPublic: true } => "public",
        { IsFamilyOrAssembly: true } => "protected internal",
        _ => "protected"
    };

    internal static string Visibility(IEnumerable<MethodInfo> accessors)
    {
        var visibilities = accessors.Where(Included).Select(public_api_signature.Visibility).ToHashSet(StringComparer.Ordinal);
        return new[] { "public", "protected internal", "protected" }.First(visibilities.Contains);
    }

    static bool IsExtensible(Type? type) => type is { IsVisible: true, IsSealed: false };
}
