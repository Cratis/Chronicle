// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_SqlStorageErrorClassifier;

public class when_classifying_schema_lock_failures : Specification
{
    readonly SqlStorageErrorClassifier _classifier = new();

    [Theory]
    [InlineData(DatabaseSchemaLockOutcome.Unknown, false)]
    [InlineData(DatabaseSchemaLockOutcome.Timeout, true)]
    [InlineData(DatabaseSchemaLockOutcome.Deadlock, true)]
    [InlineData(DatabaseSchemaLockOutcome.Canceled, false)]
    [InlineData(DatabaseSchemaLockOutcome.Error, false)]
    void should_classify_structured_outcomes_through_wrappers(DatabaseSchemaLockOutcome outcome, bool transient)
    {
        var error = new DatabaseSchemaLockNotAcquired("database", "resource", "not a classification signal", outcome);
        _classifier.IsTransient(error).ShouldEqual(transient);
        _classifier.IsTransient(new InvalidOperationException("outer", error)).ShouldEqual(transient);
        _classifier.IsTransient(new AggregateException(new FormatException(), error)).ShouldEqual(transient);
    }

    [Fact]
    void should_not_infer_a_transient_outcome_from_legacy_reason_text()
    {
        var error = new DatabaseSchemaLockNotAcquired("database", "resource", "timed out after 00:05:00");
        error.Outcome.ShouldEqual(DatabaseSchemaLockOutcome.Unknown);
        _classifier.IsTransient(error).ShouldBeFalse();
    }

    [Fact]
    void should_preserve_the_legacy_constructor_diagnostic_text() =>
        new DatabaseSchemaLockNotAcquired("database", "resource", "reason").Message.ShouldEqual(
            "DatabaseSchemaLockNotAcquired: the schema lock 'resource' of database 'database' was not acquired: reason. Another kernel may still be changing its schema.");

    [Fact]
    void should_still_inspect_transient_siblings_of_a_nontransient_lock_result() =>
        _classifier.IsTransient(new AggregateException(
            new DatabaseSchemaLockNotAcquired("database", "resource", "canceled", DatabaseSchemaLockOutcome.Canceled),
            new TimeoutException())).ShouldBeTrue();
}
