// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMessageIds;

public class when_formatting_the_id : given.an_identity
{
    const string Golden = "msg_0917f1966873eaea150d0d3d0c7ee717da19496c6f6da3aa0ad307a9032c5185";
    string _id;

    void Because() => _id = WebhookMessageIds.For(Create(epoch: _epoch));

    [Fact] void should_start_with_the_message_prefix() => _id.StartsWith("msg_", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_be_sixty_eight_characters() => _id.Length.ShouldEqual(68);
    [Fact] void should_not_contain_a_dot() => _id.Contains('.').ShouldBeFalse();
    [Fact] void should_be_lowercase_hex_after_the_prefix() => _id[4..].All(char.IsAsciiHexDigitLower).ShouldBeTrue();
    [Fact] void should_match_the_pinned_v1_value() => _id.ShouldEqual(Golden);
}
