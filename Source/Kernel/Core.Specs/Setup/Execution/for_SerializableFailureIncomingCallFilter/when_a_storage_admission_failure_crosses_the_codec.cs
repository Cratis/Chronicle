// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Setup.Serialization;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Execution.for_SerializableFailureIncomingCallFilter;

public class when_a_storage_admission_failure_crosses_the_codec : Specification
{
    Exception _filtered = null!;
    Exception _restored = null!;
    Exception _oldWrapper = null!;
    Exception _unknownCategory = null!;
    Exception _malformedCategory = null!;
    Exception _differentMessage = null!;

    async Task Because()
    {
        var context = Substitute.For<IIncomingGrainCallContext>();
        context.Invoke().Returns(Task.FromException(new MongoWaitQueueFullException("Planted storage admission overload")));
        _filtered = await Catch.Exception(() => new SerializableFailureIncomingCallFilter().Invoke(context))
            ?? throw new InvalidOperationException("The call did not fail");

        var services = new ServiceCollection();
        services.AddSerializer(_ => { });
        services.AddExceptionSerialization();
        await using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<Serializer>();
        _restored = RoundTrip(serializer, _filtered);
        _oldWrapper = RoundTrip(serializer, new InvalidOperationException("MongoDB.Driver.MongoWaitQueueFullException: Planted storage admission overload"));
        var unknown = new InvalidOperationException("MongoDB.Driver.MongoWaitQueueFullException: Planted storage admission overload");
        unknown.Data["Cratis.Chronicle.StorageAdmission.Category"] = "MongoWaitQueueFull.v2";
        _unknownCategory = RoundTrip(serializer, unknown);
        var malformed = new InvalidOperationException("MongoDB.Driver.MongoWaitQueueFullException: Planted storage admission overload");
        malformed.Data["Cratis.Chronicle.StorageAdmission.Category"] = " MongoWaitQueueFull.v1 ";
        _malformedCategory = RoundTrip(serializer, malformed);
        var differentMessage = new InvalidOperationException("a different diagnostic");
        differentMessage.Data["Cratis.Chronicle.StorageAdmission.Category"] = _filtered.Data["Cratis.Chronicle.StorageAdmission.Category"];
        _differentMessage = RoundTrip(serializer, differentMessage);
    }

    [Fact] void should_use_the_existing_exact_wrapper_before_serialization() => _filtered.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_preserve_the_filtered_runtime_type() => _restored.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_filter_to_one_category_entry() => _filtered.Data.Count.ShouldEqual(1);
    [Fact] void should_preserve_one_category_entry() => _restored.Data.Count.ShouldEqual(1);
    [Fact] void should_generate_a_string_key() => _filtered.Data.Keys.Cast<object>().Single().ShouldBeOfExactType<string>();
    [Fact] void should_preserve_a_string_key() => _restored.Data.Keys.Cast<object>().Single().ShouldBeOfExactType<string>();
    [Fact] void should_generate_a_string_category() => _filtered.Data["Cratis.Chronicle.StorageAdmission.Category"].ShouldBeOfExactType<string>();
    [Fact] void should_preserve_a_string_category() => _restored.Data["Cratis.Chronicle.StorageAdmission.Category"].ShouldBeOfExactType<string>();
    [Fact] void should_generate_the_literal_wire_pair() => _filtered.Data["Cratis.Chronicle.StorageAdmission.Category"].ShouldEqual("MongoWaitQueueFull.v1");
    [Fact] void should_preserve_the_literal_wire_pair() => _restored.Data["Cratis.Chronicle.StorageAdmission.Category"].ShouldEqual("MongoWaitQueueFull.v1");
    [Fact] void should_not_have_a_driver_inner_before_serialization() => _filtered.InnerException.ShouldBeNull();
    [Fact] void should_classify_the_filtered_failure_as_transient() => SiblingSiloInstability.IsTransient(_filtered).ShouldBeTrue();
    [Fact] void should_keep_the_filtered_message() => _restored.Message.ShouldEqual(_filtered.Message);
    [Fact] void should_keep_the_original_storage_diagnostics() => _restored.Message.ShouldEqual("MongoDB.Driver.MongoWaitQueueFullException: Planted storage admission overload");
    [Fact] void should_not_keep_a_driver_inner_exception() => _restored.InnerException.ShouldBeNull();
    [Fact] void should_preserve_transient_storage_admission_identity() => SiblingSiloInstability.IsTransient(_restored).ShouldBeTrue();
    [Fact] void should_not_reclassify_storage_admission_as_a_timeout() => (_restored is TimeoutException).ShouldBeFalse();
    [Fact] void should_classify_the_filtered_failure_as_handling() => _filtered.ToFailureKind().ShouldEqual(FailureKind.Handling);
    [Fact] void should_classify_the_restored_failure_as_handling() => _restored.ToFailureKind().ShouldEqual(FailureKind.Handling);
    [Fact] void should_keep_an_old_wrapper_as_an_ordinary_wrapper() => _oldWrapper.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_keep_an_old_wrapper_untagged() => _oldWrapper.Data.Count.ShouldEqual(0);
    [Fact] void should_keep_an_old_wrapper_without_an_inner() => _oldWrapper.InnerException.ShouldBeNull();
    [Fact] void should_preserve_the_old_wrapper_diagnostics() => _oldWrapper.Message.ShouldEqual("MongoDB.Driver.MongoWaitQueueFullException: Planted storage admission overload");
    [Fact] void should_not_classify_an_old_driver_message_as_transient() => SiblingSiloInstability.IsTransient(_oldWrapper).ShouldBeFalse();
    [Fact] void should_keep_an_unknown_category_as_an_ordinary_wrapper() => _unknownCategory.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_preserve_the_unknown_string_category() => _unknownCategory.Data["Cratis.Chronicle.StorageAdmission.Category"].ShouldEqual("MongoWaitQueueFull.v2");
    [Fact] void should_not_classify_an_unknown_category_as_transient() => SiblingSiloInstability.IsTransient(_unknownCategory).ShouldBeFalse();
    [Fact] void should_keep_a_malformed_category_as_an_ordinary_wrapper() => _malformedCategory.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_preserve_the_malformed_string_category() => _malformedCategory.Data["Cratis.Chronicle.StorageAdmission.Category"].ShouldEqual(" MongoWaitQueueFull.v1 ");
    [Fact] void should_not_classify_a_malformed_category_as_transient() => SiblingSiloInstability.IsTransient(_malformedCategory).ShouldBeFalse();
    [Fact] void should_keep_a_changed_message() => _differentMessage.Message.ShouldEqual("a different diagnostic");
    [Fact] void should_recognize_the_category_without_a_driver_message() => SiblingSiloInstability.IsTransient(_differentMessage).ShouldBeTrue();

    static Exception RoundTrip(Serializer serializer, Exception failure) => serializer.Deserialize<Exception>(serializer.SerializeToArray<Exception>(failure))!;
}
