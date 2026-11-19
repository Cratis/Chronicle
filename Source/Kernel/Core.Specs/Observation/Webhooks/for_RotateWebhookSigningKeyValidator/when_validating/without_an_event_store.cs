// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKeyValidator.when_validating;

public class without_an_event_store : Specification
{
    bool _emptyValid;
    bool _whitespaceValid;
    bool _nullValid;

    async Task Because()
    {
        var validator = new RotateWebhookSigningKeyValidator();
        _emptyValid = (await validator.ValidateAsync(new(string.Empty))).IsValid;
        _whitespaceValid = (await validator.ValidateAsync(new("  "))).IsValid;
        _nullValid = (await validator.ValidateAsync(new(null!))).IsValid;
    }

    [Fact] void should_reject_an_empty_store() => _emptyValid.ShouldBeFalse();
    [Fact] void should_reject_a_whitespace_store() => _whitespaceValid.ShouldBeFalse();
    [Fact] void should_reject_a_null_store() => _nullValid.ShouldBeFalse();
}
