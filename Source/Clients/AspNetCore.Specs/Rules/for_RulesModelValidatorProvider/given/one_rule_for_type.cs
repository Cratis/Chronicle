// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Rules;

namespace Cratis.Chronicle.AspNetCore.Rules.for_RulesModelValidatorProvider.given;

public class one_rule_for_type : Specification
{
    protected RulesModelValidatorProvider _provider;
    protected IRules _rules;
    protected IServiceProvider _serviceProvider;

    void Establish()
    {
        _rules = Substitute.For<IRules>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _provider = new(_rules, _serviceProvider);
    }
}
