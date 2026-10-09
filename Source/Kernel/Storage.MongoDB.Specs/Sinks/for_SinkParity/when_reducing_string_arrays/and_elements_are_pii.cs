// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity.when_reducing_string_arrays;

[Collection(MongoDBCollection.Name)]
public class and_elements_are_pii(MongoDBFixture fixture) : given.a_string_array_round_trip(fixture)
{
    protected override string ArrayMetadata => string.Empty;
    protected override string ItemMetadata => "\"compliance\": [{\"metadataType\":\"PII\",\"details\":\"\"}],";

    Task Because() => ApplyStates();

    [Fact] void should_read_every_prior_state_through_typed_json() => Reductions.ShouldEqual(16);
    [Fact] void should_keep_both_sinks_equivalent() => ParityReport.ShouldEqual(string.Empty);
    [Fact] void should_store_each_involved_user_in_the_array() => StoredDocument!["involvedUsers"].AsBsonArray.Count.ShouldEqual(2);
    [Fact] void should_store_each_assignee_login_in_the_array() => StoredDocument!["assigneeLogins"].AsBsonArray.Count.ShouldEqual(2);
    [Fact] void should_encrypt_each_involved_user() => StoredDocument!["involvedUsers"].AsBsonArray.All(value => new Encryption().IsEncrypted(Convert.FromBase64String(value.AsString))).ShouldBeTrue();
    [Fact] void should_encrypt_each_assignee_login() => StoredDocument!["assigneeLogins"].AsBsonArray.All(value => new Encryption().IsEncrypted(Convert.FromBase64String(value.AsString))).ShouldBeTrue();
}
