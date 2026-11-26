// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_optional_query_store_arguments_are_omitted : given.a_query_operation_policy
{
    bool _system;
    bool _ordinary;
    bool _null;
    bool _blank;

    void Establish() => _enforce = true;

    async Task Because()
    {
        _system = await Allows(nameof(given.operation_queries.Optional));
        _ordinary = await Allows(nameof(given.operation_queries.OrdinaryDefault));
        _null = await Allows(nameof(given.operation_queries.NullDefault));
        _blank = await Allows(nameof(given.operation_queries.BlankDefault));
    }

    [Fact] void should_authorize_the_effective_system_default() => _system.ShouldBeFalse();
    [Fact] void should_authorize_the_effective_ordinary_default() => _ordinary.ShouldBeTrue();
    [Fact] void should_refuse_a_null_default() => _null.ShouldBeFalse();
    [Fact] void should_refuse_a_blank_default() => _blank.ShouldBeFalse();
}
