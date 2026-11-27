// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.for_NamespaceRemovalStatusAuthorization;

public class when_a_manager_inspects_removal_status : given.a_status_call
{
    void Establish() => _granted = true;

    async Task Because() => await Inspect();

    [Fact] void should_authorize_the_manager() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_require_manage_not_read() => _asked.Single().Capability.ShouldEqual(WellKnownCapabilities.ManageEventStore);
    [Fact] void should_return_the_observed_operation() => _result.Data.OperationId.ShouldEqual(_state.OperationId);
    [Fact] void should_return_the_observed_revision() => _result.Data.Revision.ShouldEqual(_state.Revision);
    [Fact] void should_not_ensure_or_mutate_the_namespace() => _incarnations.ReceivedCalls().All(call => call.GetMethodInfo().Name == nameof(_incarnations.GetStorage)).ShouldBeTrue();
    [Fact] void should_not_have_exception_details() => _result.ExceptionMessages.ShouldBeEmpty();
}
