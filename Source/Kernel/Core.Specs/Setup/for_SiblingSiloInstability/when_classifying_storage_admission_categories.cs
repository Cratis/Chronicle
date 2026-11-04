// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Setup.for_SiblingSiloInstability;

public class when_classifying_storage_admission_categories : Specification
{
    const string CategoryKey = "Cratis.Chronicle.StorageAdmission.Category";
    const string Category = "MongoWaitQueueFull.v1";
    const string DriverDiagnostic = "MongoDB.Driver.MongoWaitQueueFullException: pool is full";

    [Fact] void should_recognize_the_exact_wrapper_and_string_pair() => Recognizes(new InvalidOperationException(DriverDiagnostic), CategoryKey, Category).ShouldBeTrue();
    [Fact] void should_not_require_a_driver_message() => Recognizes(new InvalidOperationException("a different diagnostic"), CategoryKey, Category).ShouldBeTrue();

    [Fact] void should_ignore_unrelated_metadata()
    {
        var wrapper = new InvalidOperationException("unrelated diagnostic");
        wrapper.Data["other"] = 123;
        Recognizes(wrapper, CategoryKey, Category).ShouldBeTrue();
    }

    [Fact] void should_reject_an_old_untagged_wrapper() => SiblingSiloInstability.IsTransient(new InvalidOperationException("ordinary failure")).ShouldBeFalse();
    [Fact] void should_not_infer_a_category_from_a_driver_message() => SiblingSiloInstability.IsTransient(new InvalidOperationException(DriverDiagnostic)).ShouldBeFalse();
    [Fact] void should_reject_a_missing_category_key() => Recognizes(new InvalidOperationException(DriverDiagnostic), "other", Category).ShouldBeFalse();
    [Fact] void should_reject_a_wrong_case_key() => Recognizes(new InvalidOperationException(DriverDiagnostic), "cratis.Chronicle.StorageAdmission.Category", Category).ShouldBeFalse();
    [Fact] void should_reject_a_padded_key() => Recognizes(new InvalidOperationException(DriverDiagnostic), CategoryKey + " ", Category).ShouldBeFalse();
    [Fact] void should_reject_a_lookalike_namespace_key() => Recognizes(new InvalidOperationException(DriverDiagnostic), "Foreign.Chronicle.StorageAdmission.Category", Category).ShouldBeFalse();
    [Fact] void should_reject_a_non_string_key() => Recognizes(new InvalidOperationException(DriverDiagnostic), 123, Category).ShouldBeFalse();
    [Fact] void should_not_coerce_an_object_key_to_a_string() => Recognizes(new InvalidOperationException(DriverDiagnostic), new StringLookalike(CategoryKey), Category).ShouldBeFalse();
    [Fact] void should_reject_a_null_category() => Recognizes(new InvalidOperationException(DriverDiagnostic), CategoryKey, null).ShouldBeFalse();
    [Fact] void should_reject_an_empty_category() => Recognizes(new InvalidOperationException(DriverDiagnostic), CategoryKey, string.Empty).ShouldBeFalse();
    [Fact] void should_reject_a_non_string_category() => Recognizes(new InvalidOperationException(DriverDiagnostic), CategoryKey, 123).ShouldBeFalse();
    [Fact] void should_not_coerce_an_object_category_to_a_string() => Recognizes(new InvalidOperationException(DriverDiagnostic), CategoryKey, new StringLookalike(Category)).ShouldBeFalse();
    [Fact] void should_reject_an_unknown_category() => Recognizes(new InvalidOperationException(DriverDiagnostic), CategoryKey, "MongoWaitQueueFull.v2").ShouldBeFalse();
    [Fact] void should_reject_a_wrong_case_category() => Recognizes(new InvalidOperationException(DriverDiagnostic), CategoryKey, "mongoWaitQueueFull.v1").ShouldBeFalse();
    [Fact] void should_reject_a_leading_space_in_the_category() => Recognizes(new InvalidOperationException(DriverDiagnostic), CategoryKey, " " + Category).ShouldBeFalse();
    [Fact] void should_reject_a_trailing_space_in_the_category() => Recognizes(new InvalidOperationException(DriverDiagnostic), CategoryKey, Category + " ").ShouldBeFalse();
    [Fact] void should_not_treat_a_driver_type_name_as_a_category() => Recognizes(new InvalidOperationException(DriverDiagnostic), CategoryKey, "MongoDB.Driver.MongoWaitQueueFullException").ShouldBeFalse();
    [Fact] void should_reject_a_different_wrapper_type() => Recognizes(new ArgumentException(DriverDiagnostic), CategoryKey, Category).ShouldBeFalse();
    [Fact] void should_reject_a_foreign_exception_type() => Recognizes(new Foreign.Driver.PoolExhaustedException(DriverDiagnostic), CategoryKey, Category).ShouldBeFalse();
    [Fact] void should_reject_an_invalid_operation_subclass() => Recognizes(new Foreign.Driver.DerivedInvalidOperationException(DriverDiagnostic), CategoryKey, Category).ShouldBeFalse();
    [Fact] void should_reject_a_wrapper_with_an_inner_exception() => Recognizes(new InvalidOperationException(DriverDiagnostic, new Exception("inner failure")), CategoryKey, Category).ShouldBeFalse();
    [Fact] void should_not_infer_admission_from_a_driver_inner_exception() => SiblingSiloInstability.IsTransient(new InvalidOperationException("outer failure", new MongoWaitQueueFullException("pool is full"))).ShouldBeFalse();

    [Fact] void should_not_promote_a_categorized_inner_wrapper()
    {
        var inner = new InvalidOperationException(DriverDiagnostic);
        inner.Data[CategoryKey] = Category;
        SiblingSiloInstability.IsTransient(new InvalidOperationException("outer failure", inner)).ShouldBeFalse();
    }

    static bool Recognizes(Exception exception, object key, object? value)
    {
        exception.Data[key] = value;
        return SiblingSiloInstability.IsTransient(exception);
    }

    sealed class StringLookalike(string value)
    {
        public override string ToString() => value;
    }
}
