// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Rules.for_RulesProjections;

public class SecondRule : RulesFor<SecondRule, object>
{
    public static RuleId RuleIdentifier = "f97e4ff5-7b86-4927-ae14-d6c567b78851";

    public override RuleId Identifier => RuleIdentifier;
}
