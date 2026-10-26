// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkResultFilter;

public class when_unit_of_work_is_registered : Specification
{
    MvcOptions _options;

    void Because()
    {
        using var services = new ServiceCollection().AddUnitOfWork().BuildServiceProvider();
        _options = services.GetRequiredService<IOptions<MvcOptions>>().Value;
    }

    [Fact] void should_register_the_action_signal() => _options.Filters.OfType<IAsyncActionFilter>().Count().ShouldEqual(1);
    [Fact] void should_register_the_page_signal() => _options.Filters.OfType<IAsyncPageFilter>().Count().ShouldEqual(1);
    [Fact] void should_register_an_always_run_result_filter() => _options.Filters.OfType<IAsyncAlwaysRunResultFilter>().Count().ShouldEqual(1);
    [Fact] void should_use_the_documented_order() => _options.Filters.OfType<UnitOfWorkResultFilter>().Single().Order.ShouldEqual(UnitOfWorkResultFilter.CompletionOrder);
    [Fact] void should_use_the_maximum_order() => UnitOfWorkResultFilter.CompletionOrder.ShouldEqual(int.MaxValue);
}
