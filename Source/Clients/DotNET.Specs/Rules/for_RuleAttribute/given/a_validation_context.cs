// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Rules.for_Rules.for_RuleAttribute.given;

public class a_validation_context : Specification
{
    protected IServiceProvider _serviceProvider;
    protected IRules _rules;
    protected ValidationContext _validationContext;
    protected object _modelInstance;

    void Establish()
    {
        _rules = Substitute.For<IRules>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(IRules)).Returns(_rules);

        _modelInstance = new();
        _validationContext = new(_modelInstance, _serviceProvider, new Dictionary<object, object?>());
    }
}
