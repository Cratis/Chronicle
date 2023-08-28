// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Rules.for_RulesProjections.given;

public class a_rules_projections : all_dependencies
{
    protected RulesProjections rules_projections;

    void Establish()
    {
        rules_projections = new(
            service_provider.Object,
            client_artifacts.Object,
            event_types.Object,
            model_name_resolver.Object,
            json_schema_generator.Object,
            serializer_options);
    }
}
