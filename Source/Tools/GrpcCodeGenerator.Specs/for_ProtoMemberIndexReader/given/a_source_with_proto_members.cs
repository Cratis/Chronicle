// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ProtoMemberIndexReader.given;

/// <summary>
/// Base context providing a C# source with [ProtoMember] attributes on a class.
/// </summary>
public class a_source_with_proto_members : Specification
{
    protected const string ClassName = "RegisterProductRequest";
    protected string _source = null!;

    void Establish() =>
        _source = """
            using ProtoBuf;

            namespace Generated.TestAssembly.Catalog;

            /// <summary>Represents the RegisterProductRequest message.</summary>
            [ProtoContract]
            public class RegisterProductRequest
            {
                /// <summary>Gets or sets the id.</summary>
                [ProtoMember(1)]
                public Guid Id { get; set; }

                /// <summary>Gets or sets the name.</summary>
                [ProtoMember(2)]
                public string? Name { get; set; }
            }
            """;
}
