// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_values_are_released : given.a_factory
{
    void Establish()
    {
        _definition = new("event", ["name", "number", "identifier", "day"]);
        _event = _event with { Context = _event.Context with { Subject = "explicit-subject" } };
        _released = JsonNode.Parse("""{"name":"","number":42,"identifier":"ABCDEF01-2345-6789-ABCD-EF0123456789","day":"2025-02-03"}""")!.AsObject();
        _statuses["name"] = ProtectedValueReleaseStatus.Decrypted;
        _statuses["number"] = ProtectedValueReleaseStatus.Decrypted;
        _statuses["identifier"] = ProtectedValueReleaseStatus.Decrypted;
        _statuses["day"] = ProtectedValueReleaseStatus.Decrypted;
    }

    async Task Because() => _result = await Create();

    [Fact] void should_use_the_explicit_subject() => _identifier.ShouldEqual("explicit-subject");
    [Fact] async Task should_load_the_stored_generation() => await _schemas.Received(1).GetFor("event", 2);
    [Fact] void should_pass_ciphertext_to_release_before_conversion() => _releaseInput["number"]!.GetValue<string>().ShouldEqual("opaque integer");
    [Fact] void should_keep_released_empty_strings_claimable() => _result.ShouldSkipClaimOrRemoval.ShouldBeFalse();
    [Fact] void should_preserve_the_empty_string() => _result.Components[0].Value.ShouldEqual("");
    [Fact] void should_convert_released_numeric_pii() => _result.Components[1].Value.ShouldEqual(42);
    [Fact] void should_convert_released_encrypted_guids() => _result.Components[2].Value.ShouldEqual(new Guid("abcdef01-2345-6789-abcd-ef0123456789"));
    [Fact] void should_convert_released_encrypted_dates() => _result.Components[3].Value.ShouldEqual(new DateOnly(2025, 2, 3));
    [Fact] void should_leave_stored_content_unchanged() => CurrentContent().ShouldEqual(_original);
    [Fact] void should_match_typed_component_encoding() => UniqueConstraintEncoder.Encode(Encoding.ASCII.GetBytes("slice-2b-component-key"), _result.Components).ShouldEqual(
        UniqueConstraintEncoder.Encode(Encoding.ASCII.GetBytes("slice-2b-component-key"),
            [new(""), new(42, "int32"), new(new Guid("abcdef01-2345-6789-abcd-ef0123456789"), "guid"), new(new DateOnly(2025, 2, 3), "date")]));
}
