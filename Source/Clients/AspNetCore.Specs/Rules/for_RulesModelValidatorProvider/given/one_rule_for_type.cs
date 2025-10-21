// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Rules;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Rules.for_RulesModelValidatorProvider.given;

public class one_rule_for_type : Specification
{
    protected RulesModelValidatorProvider _provider;
    protected IRules _rules;
    protected IServiceProvider _serviceProvider;
    protected IServiceProvider _scopedServiceProvider;
    protected IHttpContextAccessor _httpContextAccessor;

    void Establish()
    {
        _rules = Substitute.For<IRules>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _serviceProvider.GetService(typeof(IHttpContextAccessor)).Returns(_httpContextAccessor);
        var httpContext = Substitute.For<HttpContext>();
        _httpContextAccessor.HttpContext.Returns(httpContext);
        _scopedServiceProvider = Substitute.For<IServiceProvider>();
        httpContext.RequestServices.Returns(_scopedServiceProvider);
        _scopedServiceProvider.GetService(typeof(IRules)).Returns(_rules);
        _provider = new(_serviceProvider);
    }
}
