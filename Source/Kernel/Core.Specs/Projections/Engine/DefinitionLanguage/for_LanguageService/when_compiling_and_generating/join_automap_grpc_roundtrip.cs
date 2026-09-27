// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Services.Projections.Definitions;
using ProtoBuf;
using ContractJoin = Cratis.Chronicle.Contracts.Projections.JoinDefinition;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_and_generating;

public class join_automap_grpc_roundtrip : Specification
{
    JoinDefinition _restored;
    JoinDefinition _legacy;

    void Because()
    {
        var join = new JoinDefinition((PropertyPath)"joinId", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, AutoMap.Disabled);
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, join.ToContract());
        stream.Position = 0;
        _restored = Serializer.Deserialize<ContractJoin>(stream).ToChronicle();
        _legacy = new ContractJoin { On = "joinId", Key = string.Empty }.ToChronicle();
    }

    [Fact] void should_keep_disabled_automap_across_grpc() => _restored.AutoMap.ShouldEqual(AutoMap.Disabled);
    [Fact] void should_inherit_for_older_wire_payloads() => _legacy.AutoMap.ShouldEqual(AutoMap.Inherit);
}
