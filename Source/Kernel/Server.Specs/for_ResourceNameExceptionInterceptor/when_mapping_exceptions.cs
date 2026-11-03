// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;
using Grpc.Core;

namespace Cratis.Chronicle.Server.for_ResourceNameExceptionInterceptor;

public class when_mapping_exceptions : Specification
{
    Status? _invalid;
    Status? _conflict;
    Status? _quarantined;
    Status? _notReady;
    Status? _wrapped;
    Status? _unrelated;

    void Because()
    {
        _invalid = ResourceNameExceptionInterceptor.ToStatus(InvalidResourceName.ForEventStore(ResourceNameViolation.ReservedCharacter));
        _conflict = ResourceNameExceptionInterceptor.ToStatus(new ResourceNameConflict(ResourceNameConflictKind.Overlap));
        _quarantined = ResourceNameExceptionInterceptor.ToStatus(new ResourceQuarantined());
        _notReady = ResourceNameExceptionInterceptor.ToStatus(new ResourceNamesNotReady());
        _wrapped = ResourceNameExceptionInterceptor.ToStatus(new AggregateException(new InvalidOperationException("x", new ResourceQuarantined())));
        _unrelated = ResourceNameExceptionInterceptor.ToStatus(new InvalidOperationException());
    }

    [Fact] void should_map_a_violation_to_invalid_argument() => _invalid!.Value.StatusCode.ShouldEqual(StatusCode.InvalidArgument);
    [Fact] void should_map_a_conflict_to_already_exists() => _conflict!.Value.StatusCode.ShouldEqual(StatusCode.AlreadyExists);
    [Fact] void should_map_quarantine_to_failed_precondition() => _quarantined!.Value.StatusCode.ShouldEqual(StatusCode.FailedPrecondition);
    [Fact] void should_map_not_ready_to_unavailable() => _notReady!.Value.StatusCode.ShouldEqual(StatusCode.Unavailable);
    [Fact] void should_map_wrapped_exceptions() => _wrapped!.Value.StatusCode.ShouldEqual(StatusCode.FailedPrecondition);
    [Fact] void should_ignore_other_exceptions() => _unrelated.ShouldBeNull();
    [Fact] void should_use_the_fixed_message() => _invalid!.Value.Detail.ShouldEqual(ResourceNames.EventStoreRuleMessage);
}
