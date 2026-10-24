// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// Inventories payload types reachable from active grain-call arguments and results.
/// </summary>
public static class PayloadInventory
{
    /// <summary>
    /// Builds the transitive inventory using the kernel's custom-converter selection.
    /// </summary>
    /// <param name="grainInterfaces">The active interfaces resolved from OrleansContracts.txt.</param>
    /// <param name="hasCustomConverter">Whether the runtime has a custom converter for a type.</param>
    /// <returns>The payload snapshot.</returns>
    public static PayloadContractManifest From(IEnumerable<Type> grainInterfaces, Func<Type, bool> hasCustomConverter)
    {
        var builder = new Builder(hasCustomConverter);
        foreach (var method in grainInterfaces.SelectMany(_ => _.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)))
        {
            builder.Walk(method.ReturnType);
            foreach (var parameter in method.GetParameters())
            {
                builder.Walk(parameter.ParameterType);
            }
        }

        return new(builder.Types);
    }

    sealed class Builder(Func<Type, bool> hasCustomConverter)
    {
        readonly HashSet<Type> _visited = [];
        readonly Dictionary<string, PayloadTypeContract> _types = new(StringComparer.Ordinal);

        internal IReadOnlyDictionary<string, PayloadTypeContract> Types => _types;

        internal static bool IsCratis(Type type) => type.Namespace?.StartsWith("Cratis", StringComparison.Ordinal) == true;

        internal static bool IsExcluded(Type type) => type.IsGenericParameter
            || typeof(Exception).IsAssignableFrom(type)
            || typeof(Delegate).IsAssignableFrom(type)
            || type == typeof(CancellationToken)
            || type.GetInterfaces().Any(_ => _.FullName == "Orleans.Runtime.IAddressable")
            || type.FullName == "Orleans.Runtime.IAddressable";

        internal static Type? GenericBase(Type type, Func<Type, bool> matches)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (current.IsGenericType && matches(current.GetGenericTypeDefinition()))
                {
                    return current;
                }
            }

            return null;
        }

        internal static PayloadTypeContract DescribeEnum(Type type, string name)
        {
            var members = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(_ => new PayloadMemberContract(_.Name, Convert.ToString(_.GetRawConstantValue(), CultureInfo.InvariantCulture)!)).ToArray();
            return new(name, name, PayloadKind.Enum, null, PayloadTypeName.Of(Enum.GetUnderlyingType(type)), members);
        }

        internal void Walk(Type type)
        {
            if (!_visited.Add(type) || IsExcluded(type))
            {
                return;
            }

            if (type.HasElementType)
            {
                Walk(type.GetElementType()!);
                return;
            }

            if (type.IsGenericType && !IsCratis(type))
            {
                WalkAll(type.GetGenericArguments());
                return;
            }

            if (!IsCratis(type))
            {
                return;
            }

            var contract = Describe(type);
            _types.Add(contract.Identity, contract);
            WalkSubtypes(type, contract.Kind);
        }

        void WalkAll(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                Walk(type);
            }
        }

        void WalkSubtypes(Type type, PayloadKind kind)
        {
            if (kind != PayloadKind.Json)
            {
                return;
            }

            if (!type.IsAbstract && !type.IsInterface)
            {
                return;
            }

            var concreteTypes = type.Assembly.GetTypes().Where(_ => !_.IsAbstract && !_.ContainsGenericParameters);
            WalkAll(concreteTypes.Where(type.IsAssignableFrom));
        }

        PayloadTypeContract Describe(Type type)
        {
            var name = PayloadTypeName.Of(type);
            if (type.IsEnum)
            {
                return DescribeEnum(type, name);
            }

            if (GenericBase(type, _ => _.FullName == "Cratis.Concepts.ConceptAs`1") is { } concept)
            {
                var underlying = concept.GetGenericArguments()[0];
                Walk(underlying);
                return new(name, name, PayloadKind.Concept, null, PayloadTypeName.Of(underlying), []);
            }

            if (GenericBase(type, _ => _.Name.Contains("OneOfBase", StringComparison.Ordinal)) is { } oneOf)
            {
                return DescribeOneOf(name, oneOf);
            }

            if (hasCustomConverter(type) || Attribute.IsDefined(type, typeof(JsonConverterAttribute)))
            {
                return new(name, name, PayloadKind.Custom, null, null, []);
            }

            return DescribeMembers(type, name);
        }

        PayloadTypeContract DescribeMembers(Type type, string name)
        {
            var kind = PayloadAttributes.Find(type, "Orleans.GenerateSerializerAttribute") is null ? PayloadKind.Json : PayloadKind.Orleans;
            var alias = kind == PayloadKind.Orleans ? PayloadAttributes.Alias(type) : null;
            var properties = PayloadMembers.Read(type, kind).Select(_ => Member(_.Name, _.Type, _.Id)).ToArray();
            return new(alias ?? name, name, kind, alias, null, properties);
        }

        PayloadTypeContract DescribeOneOf(string name, Type oneOf)
        {
            var cases = oneOf.GetGenericArguments().Select((argument, index) => Member($"case{index}", argument, null)).ToArray();
            return new(name, name, PayloadKind.OneOf, null, null, cases);
        }

        PayloadMemberContract Member(string name, Type type, int? id)
        {
            Walk(type);
            return new(name, PayloadTypeName.Of(type), id);
        }
    }
}
