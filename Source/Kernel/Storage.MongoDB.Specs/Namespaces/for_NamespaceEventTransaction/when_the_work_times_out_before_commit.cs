// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_work_times_out_before_commit : given.a_namespace_transaction
{
    void Establish() => _writeFailure = new TimeoutException();
    Task Because() => ExecuteCatching();

    [Fact] void should_preserve_the_work_failure() => _failure.ShouldEqual(_writeFailure);
    [Fact] void should_not_retry_a_network_or_timeout_failure() => _attempts.ShouldEqual(1);
    [Fact] void should_abort_without_submitting_commit() => _steps.SequenceEqual(["fence", "write", "abort"]).ShouldBeTrue();
}
