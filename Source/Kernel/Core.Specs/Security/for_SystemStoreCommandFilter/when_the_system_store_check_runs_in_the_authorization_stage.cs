// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Security.for_SystemStoreCommandFilter;

public class when_the_system_store_check_runs_in_the_authorization_stage : given.a_command_filter
{
    ICommandFilter[] _authorizationFilters;
    CommandResult? _result;

    void Establish()
    {
        _caller = CallerNamed(OrdinaryUser);
        _authorizationFilters = typeof(SystemStoreCommandFilter).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true } && typeof(IAuthorizationCommandFilter).IsAssignableFrom(type))
            .Select(type => (ICommandFilter)Activator.CreateInstance(type)!)
            .ToArray();
    }

    async Task Because() => _result = await _authorizationFilters.OfType<SystemStoreAuthorizationCommandFilter>().Single().OnExecution(ContextFor(Retry("System")));

    [Fact] void should_have_one_system_store_authorization_filter_in_the_kernel() => _authorizationFilters.OfType<SystemStoreAuthorizationCommandFilter>().Count().ShouldEqual(1);
    [Fact] void should_refuse_a_caller_who_is_not_an_administrator() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_be_the_filter_that_checks_routing_values() => _authorizationFilters.OfType<SystemStoreAuthorizationCommandFilter>().Single().GetType().ShouldNotEqual(typeof(SystemStoreCommandFilter));
}
