// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Rules.for_RulesModelValidatorProvider.given;

public class one_rule_for_type : Specification
{
    protected RulesModelValidatorProvider provider;
    protected Mock<IServiceProvider> service_provider;
    protected Mock<IRules> rules;

    void Establish()
    {
        rules = new();
        service_provider = new();
        service_provider.Setup(_ => _.GetService(typeof(IRules))).Returns(rules.Object);
        provider = new(service_provider.Object);
    }
}
