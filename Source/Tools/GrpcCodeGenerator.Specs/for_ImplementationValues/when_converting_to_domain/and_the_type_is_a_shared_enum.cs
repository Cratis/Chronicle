// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;
using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ImplementationValues.when_converting_to_domain;

/// <summary>
/// A command/query parameter declared as a Core-owned shared enum arrives as the generated contract mirror, a
/// distinct CLR type - converting it back is a plain cast, the mirror image of the response-side conversion in
/// ImplementationDataMapping.
/// </summary>
[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_type_is_a_shared_enum : Specification
{
    string _result = null!;

    void Establish() => SharedTypeRegistry.Configure(2, "Cratis.Chronicle.Contracts");
    void Because() => _result = ImplementationValues.ToDomain("request.Status", typeof(CoreOwnedStatus));

    [Fact] void should_cast_to_the_domain_type() =>
        _result.ShouldEqual("(global::Cratis.Chronicle.SharedTypeCatalog.CoreOwnedStatus)request.Status");
}
