// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Rules.for_Rules.given;

public class no_rules : all_dependencies
{
    protected Rules rules;

    void Establish() => rules = new(
        json_serializer_options,
        rules_projections.Object,
        projections.Object,
        client_artifacts.Object);
}
