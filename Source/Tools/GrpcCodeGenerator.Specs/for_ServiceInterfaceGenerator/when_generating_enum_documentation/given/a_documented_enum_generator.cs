// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_enum_documentation.given;

public class a_documented_enum_generator : when_generating_a_shared_type.given.a_shared_type_generator
{
    protected string _code = null!;

    void Establish() => SharedTypeRegistry.QualifiedNameFor(typeof(CoreOwnedStatus));
}
