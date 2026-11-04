// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Setup.Execution.for_SerializableFailureIncomingCallFilter;

public class when_a_call_fails : Specification
{
    SerializableFailureIncomingCallFilter _filter;

    void Establish() => _filter = new();

    async Task<Exception> Failing(Exception exception)
    {
        var context = Substitute.For<IIncomingGrainCallContext>();
        context.Invoke().Returns(Task.FromException(exception));
        var failure = await Catch.Exception(() => _filter.Invoke(context));
        return failure ?? throw new InvalidOperationException("The call did not fail");
    }

    [Fact] async Task should_replace_an_exception_from_a_foreign_namespace() => (await Failing(new Foreign.Driver.PoolExhaustedException("pool is full"))).ShouldBeOfExactType<InvalidOperationException>();

    [Fact] async Task should_name_the_original_type_and_keep_its_message()
    {
        var failure = await Failing(new Foreign.Driver.PoolExhaustedException("pool is full"));
        failure.Message.ShouldEqual("Foreign.Driver.PoolExhaustedException: pool is full");
    }

    [Fact] async Task should_replace_a_foreign_exception_wrapped_in_a_known_one()
    {
        var failure = await Failing(new TimeoutException("slow", new Foreign.Driver.PoolExhaustedException("pool is full")));
        failure.InnerException!.Message.ShouldEqual("Foreign.Driver.PoolExhaustedException: pool is full");
    }

    async Task<Exception> FailingWithUntrustedData(Exception exception, string category)
    {
        exception.Data["Cratis.Chronicle.StorageAdmission.Category"] = category;
        exception.Data["arbitrary"] = new object();
        exception.Data["unserializable"] = new Action(() => { });
        return await Failing(exception);
    }

    [Fact] async Task should_use_the_existing_exact_wrapper_for_storage_admission() => (await Failing(new MongoWaitQueueFullException("pool is full"))).ShouldBeOfExactType<InvalidOperationException>();

    [Fact] async Task should_assign_the_storage_admission_category() => (await Failing(new MongoWaitQueueFullException("pool is full"))).Data["Cratis.Chronicle.StorageAdmission.Category"].ShouldEqual("MongoWaitQueueFull.v1");

    [Fact] async Task should_preserve_transient_storage_admission_identity() => SiblingSiloInstability.IsTransient(await Failing(new MongoWaitQueueFullException("pool is full"))).ShouldBeTrue();

    [Fact] async Task should_keep_the_storage_admission_diagnostics() => (await Failing(new MongoWaitQueueFullException("pool is full"))).Message.ShouldEqual("MongoDB.Driver.MongoWaitQueueFullException: pool is full");

    [Fact] async Task should_not_keep_a_driver_inner_exception() => (await Failing(new MongoWaitQueueFullException("pool is full"))).InnerException.ShouldBeNull();

    [Fact] void should_still_recognize_raw_storage_admission_failures() => SiblingSiloInstability.IsTransient(new MongoWaitQueueFullException("pool is full")).ShouldBeTrue();

    [Fact] async Task should_replace_driver_data_with_only_the_generated_category() => (await FailingWithUntrustedData(new MongoWaitQueueFullException("pool is full"), "wrong-category")).Data.Count.ShouldEqual(1);

    [Fact] async Task should_generate_the_category_instead_of_copying_driver_data() => (await FailingWithUntrustedData(new MongoWaitQueueFullException("pool is full"), "wrong-category")).Data["Cratis.Chronicle.StorageAdmission.Category"].ShouldEqual("MongoWaitQueueFull.v1");

    [Fact] async Task should_discard_all_unrelated_foreign_data() => (await FailingWithUntrustedData(new Foreign.Driver.PoolExhaustedException("pool is full"), "MongoWaitQueueFull.v1")).Data.Count.ShouldEqual(0);

    [Fact] async Task should_not_promote_an_unrelated_foreign_category() => SiblingSiloInstability.IsTransient(await FailingWithUntrustedData(new Foreign.Driver.PoolExhaustedException("pool is full"), "MongoWaitQueueFull.v1")).ShouldBeFalse();

    [Fact] async Task should_not_assign_a_category_to_a_driver_subclass() => (await Failing(new Foreign.Driver.DerivedWaitQueueFullException("pool is full"))).Data.Count.ShouldEqual(0);

    [Fact] async Task should_not_promote_a_filtered_driver_subclass() => SiblingSiloInstability.IsTransient(await Failing(new Foreign.Driver.DerivedWaitQueueFullException("pool is full"))).ShouldBeFalse();

    [Fact] void should_preserve_existing_raw_driver_subclass_classification() => SiblingSiloInstability.IsTransient(new Foreign.Driver.DerivedWaitQueueFullException("pool is full")).ShouldBeTrue();

    [Fact] async Task should_preserve_supported_exception_data_without_authenticating_it()
    {
        var original = new InvalidOperationException("unrelated diagnostic");
        original.Data["Cratis.Chronicle.StorageAdmission.Category"] = "MongoWaitQueueFull.v1";
        (await Failing(original)).ShouldBeSame(original);
    }

    [Fact] async Task should_leave_a_known_exception_untouched()
    {
        var original = new TimeoutException("slow");
        (await Failing(original)).ShouldEqual(original);
    }
}
