// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;
using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ImplementationValues.when_converting_to_domain;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_type_is_a_nullable_shared_enum : Specification
{
    string _expression;

    void Establish() => SharedTypeRegistry.Configure(2, "Cratis.Chronicle.Contracts");
    void Because() => _expression = ImplementationValues.ToDomain("request.Status", typeof(CoreOwnedStatus?));

    [Fact] void should_use_a_lifted_enum_cast() => _expression.ShouldEqual("(global::Cratis.Chronicle.SharedTypeCatalog.CoreOwnedStatus?)request.Status");
}
