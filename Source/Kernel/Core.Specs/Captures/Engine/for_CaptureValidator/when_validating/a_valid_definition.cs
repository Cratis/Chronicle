// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.when_validating;

public class a_valid_definition : given.a_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    async Task Because() => _result = await _validator.Validate(_eventStore, CreateDefinition());

    [Fact] void should_have_no_messages() => _result.ShouldBeEmpty();
}
