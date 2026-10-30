// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// Mirrors Orleans' per-declaring-type field selection, including the constructor wire segment.
/// </summary>
internal static class OrleansPayloadMembers
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    internal static IEnumerable<(string Name, Type Type, uint? Id, int Level, bool Constructor)> Read(Type type)
    {
        var level = 0;
        for (var current = type; current is not null; current = current.BaseType, level++)
        {
            foreach (var member in ReadLevel(current))
            {
                yield return (member.Name, member.Type, member.Id, level, member.Constructor);
            }
        }
    }

    static IEnumerable<(string Name, Type Type, uint Id, bool Constructor)> ReadLevel(Type type)
    {
        var members = type.GetMembers(Flags).Where(_ => _ is PropertyInfo or FieldInfo).ToArray();
        var parameters = ConstructorParameters(type);
        var automatic = AutomaticIds(type) && !members.Any(_ => PayloadAttributes.Id(_) is not null) && !parameters.Any(_ => ParameterId(_) is not null);
        var explicitFields = members.OfType<FieldInfo>().Where(_ => PayloadAttributes.Id(_) is not null).ToArray();
        foreach (var field in explicitFields.Where(_ => PayloadAttributes.Find(_, "System.NonSerializedAttribute") is null))
        {
            yield return (field.Name, field.FieldType, PayloadAttributes.Id(field)!.Value, false);
        }

        var properties = members.OfType<PropertyInfo>().Where(_ => _.GetIndexParameters().Length == 0);
        foreach (var property in properties.Where(property => !explicitFields.Any(_ => _.Name == $"<{property.Name}>k__BackingField")))
        {
            if (ReadProperty(property, parameters, automatic) is { } member)
            {
                yield return member;
            }
        }
    }

    static (string Name, Type Type, uint Id, bool Constructor)? ReadProperty(PropertyInfo property, ParameterInfo[] parameters, bool automatic)
    {
        var parameter = parameters.FirstOrDefault(_ => Canonical(_.Name!) == Canonical(property.Name) && _.ParameterType == property.PropertyType);
        if (automatic)
        {
            return AutomaticProperty(property, parameter is not null);
        }

        if (PayloadAttributes.Id(property) is { } id)
        {
            return (property.Name, property.PropertyType, id, false);
        }

        return parameter is null ? null : (property.Name, property.PropertyType, ParameterId(parameter) ?? (uint)parameter.Position, true);
    }

    static (string Name, Type Type, uint Id, bool Constructor)? AutomaticProperty(PropertyInfo property, bool constructor)
    {
        var backing = property.DeclaringType!.GetField($"<{property.Name}>k__BackingField", Flags);
        if (property.SetMethod is null && backing is null)
        {
            return null;
        }

        return (property.Name, property.PropertyType, PayloadFieldId.For(property.PropertyType, Canonical(property.Name)), constructor);
    }

    static bool AutomaticIds(Type type) => PayloadAttributes.Find(type, "Orleans.GenerateSerializerAttribute")?.NamedArguments
        .Any(_ => _.MemberName == "GenerateFieldIds" && Convert.ToInt32(_.TypedValue.Value, System.Globalization.CultureInfo.InvariantCulture) == 1) == true;

    static uint? ParameterId(ParameterInfo parameter)
    {
        var attribute = parameter.GetCustomAttributesData().FirstOrDefault(_ => _.AttributeType.FullName == "Orleans.IdAttribute");
        return attribute is null ? null : Convert.ToUInt32(attribute.ConstructorArguments[0].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    static ParameterInfo[] ConstructorParameters(Type type)
    {
        if (!IncludesPrimaryConstructor(type))
        {
            return [];
        }

        var constructors = type.GetConstructors(Flags).Where(_ => !Attribute.IsDefined(_, typeof(CompilerGeneratedAttribute)));
        return constructors.FirstOrDefault(_ => IsPrimaryConstructor(type, _))?.GetParameters() ?? [];
    }

    static bool IncludesPrimaryConstructor(Type type)
    {
        var include = PayloadAttributes.Find(type, "Orleans.GenerateSerializerAttribute")?.NamedArguments
            .FirstOrDefault(_ => _.MemberName == "IncludePrimaryConstructorParameters").TypedValue.Value as bool?;
        return include ?? IsRecord(type);
    }

    static bool IsRecord(Type type) => type.GetMethod("<Clone>$", Flags) is not null
        || (type.IsValueType && type.GetMethod("PrintMembers", Flags) is not null);

    static bool IsPrimaryConstructor(Type type, ConstructorInfo constructor)
    {
        var parameters = constructor.GetParameters();
        return parameters.Length > 0 && parameters.All(_ => IsPrimaryParameter(type, _));
    }

    static bool IsPrimaryParameter(Type type, ParameterInfo parameter)
    {
        var property = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(_ => Canonical(_.Name) == Canonical(parameter.Name!));
        return property?.PropertyType == parameter.ParameterType
            && property.GetMethod is { } getter && Attribute.IsDefined(getter, typeof(CompilerGeneratedAttribute));
    }

    static string Canonical(string name)
    {
        var trimmed = name.TrimStart('_');
        return trimmed.Length == 0 ? trimmed : char.ToLowerInvariant(trimmed[0]) + trimmed[1..];
    }
}
