// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_grpc_contracts.when_constructing;

public class and_the_message_has_collection_members : Specification
{
    IReadOnlyList<string> _missingValues;

    void Because() =>
        _missingValues = typeof(CommandResult).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } &&
                           type.Name.EndsWith("Response", StringComparison.Ordinal) &&
                           type.IsDefined(typeof(ProtoContractAttribute)) &&
                           type.GetConstructor(Type.EmptyTypes) is not null)
            .SelectMany(type =>
            {
                var instance = Activator.CreateInstance(type);
                return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(property => property.CanRead &&
                                       property.GetIndexParameters().Length == 0 &&
                                       IsCollection(property.PropertyType) &&
                                       property.GetValue(instance) is null)
                    .Select(property => $"{type.FullName}.{property.Name}");
            })
            .Order()
            .ToArray();

    [Fact] void should_supply_every_collection() => _missingValues.ShouldBeEmpty();

    static bool IsCollection(Type type) =>
        type != typeof(string) &&
        (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) ||
         type.GetInterfaces().Any(_ => _ == typeof(System.Collections.IEnumerable)));
}
