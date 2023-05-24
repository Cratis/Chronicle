// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Rules.for_Rules.given;

public class one_rule_for_type : all_dependencies
{
    protected Rules rules;

    protected RulesForTypeForRules rules_for_type;

    void Establish()
    {
        types.SetupGet(_ => _.All).Returns(new[] { typeof(RulesForTypeForRules) });

        rules = new(
            execution_context,
            model_name_convention.Object,
            event_types.Object,
            json_schema_generator.Object,
            json_serializer_options,
            immediate_projections.Object,
            types.Object);
    }
}
