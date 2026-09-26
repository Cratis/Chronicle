// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;
using KernelJoin = Cratis.Chronicle.Concepts.Projections.Definitions.JoinDefinition;
using MongoJoin = Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.JoinDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_JoinDefinitionConverters;

public class when_round_tripping_automap : Specification
{
    KernelJoin _restored;
    KernelJoin _legacy;

    void Because()
    {
        var definition = new KernelJoin((PropertyPath)"joinId", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, AutoMap.Disabled);
        _restored = definition.ToMongoDB().ToKernel();
        _legacy = new MongoJoin
        {
            On = (PropertyPath)"joinId",
            Properties = new Dictionary<string, string>(),
            Key = PropertyExpression.NotSet
        }.ToKernel();
    }

    [Fact] void should_preserve_the_join_setting() => _restored.AutoMap.ShouldEqual(AutoMap.Disabled);
    [Fact] void should_inherit_for_old_documents() => _legacy.AutoMap.ShouldEqual(AutoMap.Inherit);
}
