// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_a_session_uses_a_lowercase_identifier : and_the_immediate_projection_subject_differs_from_the_request_key
{
    protected override string IdentifierProperty => "id";
    protected override string SessionId => "2a8bbd7e-2670-4930-8e37-5af922fd88a0";

    [Fact] void should_use_event_lineage_in_the_session() => JsonNode.Parse(_result.ReadModel)!["name"]!.GetValue<string>().ShouldEqual("Ada Lovelace");
    [Fact] void should_preserve_the_lowercase_identifier() => JsonNode.Parse(_result.ReadModel)!["id"]!.GetValue<string>().ShouldEqual("different-source");
    [Fact] void should_release_the_other_subject_in_the_session() => JsonNode.Parse(_result.ReadModel)!["otherName"]!.GetValue<string>().ShouldEqual("other name");
    [Fact] void should_not_expose_lineage_metadata() => JsonNode.Parse(_result.ReadModel)!.AsObject().ContainsKey(WellKnownProperties.Subjects).ShouldBeFalse();
}
