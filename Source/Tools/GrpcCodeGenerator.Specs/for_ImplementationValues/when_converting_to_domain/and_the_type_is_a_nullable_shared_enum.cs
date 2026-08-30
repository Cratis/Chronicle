// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;
using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ImplementationValues.when_converting_to_domain;

/// <summary>
/// A nullable shared enum parameter (Nullable&lt;T&gt; is a generic type, so SharedTypeRegistry never treats it
/// as a candidate in its own right) used to fall all the way through to the identity branch - returning the wire
/// expression unchanged - which is wrong: the wire value is the contract enum, not the domain one. Refusing here
/// is deliberate, the same choice ImplementationDataMapping.ForNullable already makes on the response side, since
/// there is no proven cast/null-check shape for this as a request parameter yet.
/// </summary>
[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_type_is_a_nullable_shared_enum : Specification
{
    Exception? _exception;

    void Establish() => SharedTypeRegistry.Configure(2, "Cratis.Chronicle.Contracts");
    void Because() => _exception = Catch.Exception(() => ImplementationValues.ToDomain("request.Status", typeof(CoreOwnedStatus?)));

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<UnsupportedServiceShape>();
    [Fact] void should_name_the_non_nullable_form_as_the_way_out() =>
        _exception!.Message.ShouldContain("use the non-nullable form");
}
