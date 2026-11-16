// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

public class when_an_unrecoverable_pii_value_is_released_strictly : given.an_unrecoverable_pii_value
{
    Exception _failure;

    async Task Because() => _failure = await Catch.Exception(() => _manager.ReleaseStrict(EventStoreName.NotSet, EventStoreNamespaceName.Default, _schema, Subject, _input));

    [Fact] void should_preserve_the_path_failure() => _failure.ShouldBeOfExactType<SchemaMetadataActionFailed>();
    [Fact] void should_keep_the_original_pii_unwrap_failure() => _failure.InnerException.ShouldBeSame(_unwrap);
    [Fact] void should_keep_the_attempted_revision_count() => (UnrecoverableProtectedValueReporter.Find(_failure)?.AttemptedRevisions).ShouldEqual(2);
    [Fact] void should_report_the_failure_once() => _count.ShouldEqual(1);
    [Fact] void should_offer_a_revision_retention_hint() => _failure.Message.ShouldContain("key revision that is no longer available");
}
