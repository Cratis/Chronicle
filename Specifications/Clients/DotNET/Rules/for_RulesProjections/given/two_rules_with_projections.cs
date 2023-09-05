// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Aksio.Cratis.Rules.for_RulesProjections.given;

public class two_rules_with_projections : all_dependencies
{
    protected RulesProjections rules_projections;

    void Establish()
    {
        client_artifacts.SetupGet(_ => _.Rules).Returns(new[]
        {
            typeof(FirstRule),
            typeof(SecondRule)
        });

        service_provider.Setup(_ => _.GetService(typeof(FirstRule))).Returns(new FirstRule());
        service_provider.Setup(_ => _.GetService(typeof(SecondRule))).Returns(new SecondRule());

        json_schema_generator.Setup(_ => _.Generate(IsAny<Type>())).Returns(new JsonSchema());

        rules_projections = new(
            service_provider.Object,
            client_artifacts.Object,
            event_types.Object,
            model_name_resolver.Object,
            json_schema_generator.Object,
            serializer_options);
    }
}
