// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.and_typed_paths_downcast_with_custom_names.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration;

[Collection(ChronicleCollection.Name)]
public class and_typed_paths_downcast_with_custom_names(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_camel_case_client(fixture, "typed-downcast")
    {
        public override IEnumerable<Type> EventTypes => [typeof(WireContactRecordedV1), typeof(WireContactRecorded)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(TypedWireContactMigration)];
        protected override INamingPolicy NamingPolicy => new given.PrefixNamingPolicy();

        async Task Because()
        {
            Result = await _store.EventLog.Append("contact", new WireContactRecorded(new WireContactDetails("person@example.com", "123", "work")));
            await ReadStoredGenerations();
        }
    }

    [Fact] void should_register_the_migration() => Context.RegistrationSucceeded.ShouldBeTrue();
    [Fact] void should_append_successfully() => Context.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_one_event() => Context.StoredCount.ShouldEqual(1);
    [Fact] void should_migrate_the_explicit_json_name() => Context.GenerationOne["ContactDetails"]!["WireAddress"]!.GetValue<string>().ShouldEqual("person@example.com");
    [Fact] void should_migrate_the_policy_named_property() => Context.GenerationOne["ContactDetails"]!["mapped_PhoneNumber"]!.GetValue<string>().ShouldEqual("123");
    [Fact] void should_preserve_the_unmapped_sibling() => Context.GenerationOne["ContactDetails"]!["mapped_Label"]!.GetValue<string>().ShouldEqual("work");
}
