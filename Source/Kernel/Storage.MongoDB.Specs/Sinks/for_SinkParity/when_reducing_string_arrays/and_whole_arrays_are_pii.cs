// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity.when_reducing_string_arrays;

[Collection(MongoDBCollection.Name)]
public class and_whole_arrays_are_pii(MongoDBFixture fixture) : given.a_string_array_round_trip(fixture)
{
    protected override string ArrayMetadata => "\"compliance\": [{\"metadataType\":\"PII\",\"details\":\"\"}],";

    Task Because() => ApplyStates();

    [Fact] void should_read_every_prior_state_through_typed_json() => Reductions.ShouldEqual(16);
    [Fact] void should_keep_both_sinks_equivalent() => ParityReport.ShouldEqual(string.Empty);
    [Fact] void should_store_involved_users_as_ciphertext() => new Encryption().IsEncrypted(Convert.FromBase64String(StoredDocument!["involvedUsers"].AsString)).ShouldBeTrue();
    [Fact] void should_store_assignee_logins_as_ciphertext() => new Encryption().IsEncrypted(Convert.FromBase64String(StoredDocument!["assigneeLogins"].AsString)).ShouldBeTrue();
}
