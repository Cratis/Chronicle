// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Rules.for_Rules.given;

public class one_rule_for_type : all_dependencies
{
    protected Rules rules;

    protected RulesForTypeForRules rules_for_type;

    void Establish()
    {
        client_artifacts.SetupGet(_ => _.Rules).Returns(new[] { typeof(RulesForTypeForRules) });

        rules = new(
            json_serializer_options,
            rules_projections.Object,
            immediate_projections.Object,
            client_artifacts.Object);
    }
}
