// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Rules;

namespace Cratis.Chronicle.AspNetCore.Rules.for_RulesModelValidatorProvider.given;

public class one_rule_for_type : Specification
{
    protected RulesModelValidatorProvider provider;
    protected Mock<IRules> rules;
    protected Mock<IServiceProvider> service_provider;

    void Establish()
    {
        rules = new();
        service_provider = new();
        provider = new(rules.Object, service_provider.Object);
    }
}
