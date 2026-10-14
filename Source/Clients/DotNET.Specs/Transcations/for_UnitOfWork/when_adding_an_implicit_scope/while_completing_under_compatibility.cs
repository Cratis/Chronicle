// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_an_implicit_scope;

public class while_completing_under_compatibility : given.a_unit_with_a_pending_implicit_append
{
    Task Because() => AttemptLateEnrollment();
    [Fact] void should_ignore_the_late_call() => _lateError.ShouldBeNull();
    [Fact] void should_preserve_the_captured_boundary() => _captured.ImplicitScopes[(EventSourceId)"source"].Value.ShouldEqual(7UL);
}
