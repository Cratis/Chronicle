// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

internal static class public_api_attributes
{
    static readonly HashSet<string> _includedNamespaces = new(StringComparer.Ordinal)
    {
        "ProtoBuf",
        "ProtoBuf.Grpc.Configuration",
        "System.ServiceModel",
        "System.Diagnostics.CodeAnalysis"
    };

    static readonly HashSet<string> _includedTypes = new(StringComparer.Ordinal)
    {
        "System.ObsoleteAttribute",
        "System.FlagsAttribute",
        "System.ParamArrayAttribute",
        "System.Runtime.CompilerServices.RequiredMemberAttribute",
        "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute",
        "System.Runtime.CompilerServices.DecimalConstantAttribute",
        "System.Runtime.CompilerServices.DateTimeConstantAttribute",
        "System.Runtime.CompilerServices.NullableAttribute",
        "System.Runtime.CompilerServices.NullableContextAttribute",
        "System.Runtime.CompilerServices.IsReadOnlyAttribute",
        "System.Runtime.CompilerServices.IsByRefLikeAttribute",
        "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute"
    };

    internal static IEnumerable<string> Render(string owner, IEnumerable<CustomAttributeData> attributes) => attributes
        .Where(attribute => Included(attribute.AttributeType))
        .Select(attribute => $"attribute {owner} [{public_api_type_name.Render(attribute.AttributeType)}({string.Join(',', attribute.ConstructorArguments.Select(Argument))}){Named(attribute)}]");

    internal static string Value(object? value) => value switch
    {
        null => "null",
        string text => JsonSerializer.Serialize(text),
        char character => $"char:{(int)character}",
        bool boolean => boolean ? "true" : "false",
        _ => MetadataValue(value)
    };

    static string MetadataValue(object value) => value switch
    {
        Type type => $"typeof({public_api_type_name.Render(type)})",
        Missing => "missing",
        DBNull => "dbnull",
        Enum enumeration => $"{public_api_type_name.Render(enumeration.GetType())}:{enumeration:D}",
        _ => FormattedValue(value)
    };

    static string FormattedValue(object value) => value switch
    {
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => throw new NotSupportedException($"Unsupported public API constant: {value.GetType().FullName}")
    };

    static bool Included(Type type) => _includedNamespaces.Contains(type.Namespace ?? string.Empty)
        || _includedTypes.Contains(type.FullName ?? string.Empty);

    static string Argument(CustomAttributeTypedArgument argument)
    {
        var value = argument.Value is IEnumerable<CustomAttributeTypedArgument> items
            ? $"[{string.Join(',', items.Select(Argument))}]"
            : Value(argument.Value);
        return $"{public_api_type_name.Render(argument.ArgumentType)}:{value}";
    }

    static string Named(CustomAttributeData attribute) => string.Concat(attribute.NamedArguments
        .OrderBy(argument => argument.MemberName, StringComparer.Ordinal)
        .Select(argument => $" {argument.MemberName}={Argument(argument.TypedValue)}"));
}
