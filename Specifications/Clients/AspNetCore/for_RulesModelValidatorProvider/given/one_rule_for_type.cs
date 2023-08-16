// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Rules;

namespace Aksio.Cratis.AspNetCore.Rules.for_RulesModelValidatorProvider.given;

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
