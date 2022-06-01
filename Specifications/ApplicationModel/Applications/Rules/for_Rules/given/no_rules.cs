// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Rules.for_Rules.given;

public class no_rules : all_dependencies
{
    protected Rules rules;

    void Establish() => rules = new(
        execution_context,
        event_types.Object,
        json_schema_generator.Object,
        json_serializer_options,
        types.Object,
        cluster_client.Object);
}
