// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Aksio.Cratis.Rules.for_Rules.for_RuleAttribute.given;

public class a_validation_context : Specification
{
    protected Mock<IServiceProvider> service_provider;
    protected Mock<IRules> rules;
    protected ValidationContext validation_context;
    protected object model_instance;

    void Establish()
    {
        rules = new();
        service_provider = new();
        service_provider.Setup(_ => _.GetService(typeof(IRules))).Returns(rules.Object);

        model_instance = new();
        validation_context = new(model_instance, service_provider.Object, new Dictionary<object, object>());
    }
}
