// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Serialization;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.and_an_object_default_uses_a_custom_policy.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration;

[Collection(ChronicleCollection.Name)]
public class and_an_object_default_uses_a_custom_policy(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_camel_case_client(fixture, "object-default")
    {
        static readonly JsonSerializerOptions _deliveredOptions = new() { PropertyNamingPolicy = new given.PrefixNamingPolicy().JsonPropertyNamingPolicy };

        public ObjectDefaultRecorded Delivered;

        public override IEnumerable<Type> EventTypes => [typeof(ObjectDefaultRecordedV1), typeof(ObjectDefaultRecorded)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(ObjectDefaultMigration)];
        protected override INamingPolicy NamingPolicy => new given.PrefixNamingPolicy();

        async Task Because()
        {
            Result = await _store.EventLog.Append("object-default", new ObjectDefaultRecordedV1("name"));
            await ReadStoredGenerations();
            Delivered = GenerationTwo.Deserialize<ObjectDefaultRecorded>(_deliveredOptions)!;
        }
    }

    [Fact] void should_register_the_migration() => Context.RegistrationSucceeded.ShouldBeTrue();
    [Fact] void should_append_successfully() => Context.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_one_event() => Context.StoredCount.ShouldEqual(1);
    [Fact] void should_store_the_default_with_policy_named_members() => Context.GenerationTwo["mapped_Defaults"]!["mapped_Module"]!.GetValue<string>().ShouldEqual("global");
    [Fact] void should_not_store_the_default_with_unconverted_members() => Context.GenerationTwo["mapped_Defaults"]!.AsObject().ContainsKey("Module").ShouldBeFalse();
    [Fact] void should_deliver_the_typed_default() => Context.Delivered.Defaults.Module.ShouldEqual("global");
}
