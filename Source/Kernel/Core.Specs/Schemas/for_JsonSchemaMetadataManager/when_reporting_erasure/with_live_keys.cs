// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_reporting_erasure;

public class with_live_keys : given.content_for_erasure_reporting
{
    async Task Because() => await Release();

    [Fact] void should_round_trip_real_ciphertext() => _result.Value.ToJsonString().ShouldEqual(_plaintext.ToJsonString());
    [Fact] void should_not_report_an_encrypted_empty_string_as_erased() => _result.ErasedPaths.ShouldBeEmpty();
    [Fact] void should_not_mutate_stored_ciphertext() => _stored.ToJsonString().ShouldEqual(_storedText);
}
