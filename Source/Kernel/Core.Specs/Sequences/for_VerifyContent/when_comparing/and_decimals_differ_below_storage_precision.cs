// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_decimals_differ_below_storage_precision : given.a_stored_event
{
    void Establish()
    {
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"value":{"type":"number","format":"decimal"}}} """);
        _storage.GetEventStore("store").EventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, schema));
        _command = _command with { Content = "{\"value\":0.1234567890123456789012345678}" };
        _stored = _stored with { GenerationalContent = new Dictionary<int, string> { [1] = "{\"value\":0.1234567890123456789012345679}" } };
    }

    async Task Because() => _result = await _command.Handle(_storage, _manager, _converter);

    [Fact] void should_compare_only_the_precision_available_on_every_backend() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
