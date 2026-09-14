// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_grpc_contracts.when_constructing;

public class and_the_message_has_collection_members : Specification
{
    IReadOnlyList<string> _missingValues;

    void Because() =>
        _missingValues = typeof(CommandResult).Assembly
            .GetTypes()
            .Concat(typeof(CommandResult).Assembly.GetTypes()
                .Where(type => type.IsInterface)
                .SelectMany(type => type.GetMethods())
                .SelectMany(method => GetResultEnvelopes(method.ReturnType)))
            .Distinct()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } &&
                           type.IsDefined(typeof(ProtoContractAttribute)) &&
                           type.GetConstructor(Type.EmptyTypes) is not null)
            .SelectMany(type =>
            {
                var instance = Activator.CreateInstance(type);
                using var stream = new MemoryStream();
                var deserialized = Serializer.NonGeneric.Deserialize(type, stream);
                return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(property => property.CanRead &&
                                       property.GetIndexParameters().Length == 0 &&
                                       IsCollection(property.PropertyType) &&
                                       new NullabilityInfoContext().Create(property).ReadState != NullabilityState.Nullable &&
                                       (property.GetValue(instance) is null || property.GetValue(deserialized) is null))
                    .Select(property => $"{type.FullName}.{property.Name}");
            })
            .Order()
            .ToArray();

    [Fact] void should_supply_every_collection() => Assert.True(_missingValues.Count == 0, string.Join(Environment.NewLine, _missingValues));

    static IEnumerable<Type> GetResultEnvelopes(Type type)
    {
        if (!type.IsGenericType)
        {
            return [];
        }

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(QueryResult<>) || definition == typeof(CommandResult<>)
            ? [type]
            : type.GetGenericArguments().SelectMany(GetResultEnvelopes);
    }

    static bool IsCollection(Type type) =>
        type != typeof(string) &&
        (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) ||
         type.GetInterfaces().Any(_ => _ == typeof(System.Collections.IEnumerable)));
}
