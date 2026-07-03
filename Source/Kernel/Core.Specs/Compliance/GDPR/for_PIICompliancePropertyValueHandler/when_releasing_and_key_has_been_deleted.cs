// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler;

public class when_releasing_and_key_has_been_deleted : given.a_property_handler
{
    JsonNode _input;
    JsonNode _result;

    void Establish()
    {
        _input = JsonValue.Create(Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted")));
        _keyStore.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(Task.FromResult(false));
    }

    async Task Because() => _result = await _handler.Release(string.Empty, string.Empty, Identifier, _input);

    [Fact] void should_return_empty() => _result.ToString().ShouldEqual(string.Empty);
    [Fact] async Task should_not_attempt_to_decrypt() => await _keyStore.DidNotReceive().GetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
}
