// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Rules.for_RulesProjections;

public class FirstRule : RulesFor<FirstRule, object>
{
    public const string RuleIdentifier = "7faecfd4-3b93-473e-a44a-04980820a9a2";

    public override RuleId Identifier => RuleIdentifier;
}
