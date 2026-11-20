// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionConverter;

public class when_using_an_undefined_signing_mode : Specification
{
    WebhookDefinition _definition;
    Exception _exception;

    void Establish() => _definition = new("webhook", [], new WebhookTarget("https://example.com/webhook", default(OneOf.Types.None), new Dictionary<string, string>()), EventSequenceId.Log, true, true)
    {
        Signing = (WebhookSigningMode)99
    };

    void Because() => _exception = Catch.Exception(() => _definition.ToContract());

    [Fact] void should_reject_the_mode_instead_of_disabling_signing() => _exception.ShouldBeOfExactType<ArgumentOutOfRangeException>();
}
