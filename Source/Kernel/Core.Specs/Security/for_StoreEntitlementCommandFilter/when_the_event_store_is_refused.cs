// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_the_event_store_is_refused : given.a_command_filter
{
    CommandResult _unknown;
    CommandResult _notGranted;

    void Establish()
    {
        _enforce = true;
        Grant("other", StoreAccess.Append);
    }

    async Task Because()
    {
        _unknown = await _filter.OnExecution(ContextFor(new AppendLike("does-not-exist", "tenant")));
        _notGranted = await _filter.OnExecution(ContextFor(new AppendLike("shop", "tenant")));
    }

    [Fact] void should_refuse_an_unknown_store() => _unknown.IsAuthorized.ShouldBeFalse();
    [Fact] void should_refuse_a_store_that_is_not_granted() => _notGranted.IsAuthorized.ShouldBeFalse();
    [Fact] void should_use_the_same_message_for_both() => _unknown.AuthorizationFailureReason.ShouldEqual(_notGranted.AuthorizationFailureReason);
    [Fact] void should_not_name_the_store_in_the_message() => _notGranted.AuthorizationFailureReason.Contains("shop").ShouldBeFalse();
}
