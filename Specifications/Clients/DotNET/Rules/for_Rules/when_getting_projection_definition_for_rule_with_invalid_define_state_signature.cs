// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Rules.for_Rules;

public class when_getting_projection_definition_for_rule_with_invalid_define_state_signature : given.one_rule_for_type
{
    RuleWithInvalidDefineStateSignature rule;
    Exception result;

    void Establish() => rule = new();

    void Because() => result = Catch.Exception(() => rules.GetProjectionDefinitionFor(rule));

    [Fact] void should_throw_invalid_signature_for_define_state() => result.ShouldBeOfExactType<InvalidDefineStateInRuleSignature>();
}
