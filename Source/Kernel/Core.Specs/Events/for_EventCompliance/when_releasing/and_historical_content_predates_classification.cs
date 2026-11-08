// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.for_EventCompliance.when_releasing;

public class and_historical_content_predates_classification : given.a_stored_generation_with_inherited_protection
{
    JsonObject _released;

    async Task Because()
    {
        var released = await _compliance.Release(EventWith(_plaintext), _own);
        _released = _converter.ToJsonObject(released.Content, _own);
    }

    [Fact] void should_leave_historical_personal_plaintext_readable() => _released["name"]!.GetValue<string>().ShouldEqual(_plaintext["name"]!.GetValue<string>());
    [Fact] void should_leave_historical_unencrypted_plaintext_readable() => _released["secret"]!.GetValue<string>().ShouldEqual(_plaintext["secret"]!.GetValue<string>());
    [Fact] void should_preserve_the_complete_historical_content() => JsonNode.DeepEquals(_released, _plaintext).ShouldBeTrue();
}
