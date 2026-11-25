// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionBuilder;

public class when_using_an_undefined_signing_mode : Specification
{
    WebhookDefinitionBuilder _builder;
    Exception _exception;

    void Establish() => _builder = new(Substitute.For<IEventTypes>());

    void Because() => _exception = Catch.Exception(() => _builder.WithSigning((WebhookSigningMode)99));

    [Fact] void should_reject_the_mode() => _exception.ShouldBeOfExactType<ArgumentOutOfRangeException>();
}
