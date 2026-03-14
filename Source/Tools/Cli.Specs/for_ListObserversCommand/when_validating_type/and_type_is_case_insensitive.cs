// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli.Commands.Observers;

namespace Cratis.Chronicle.Cli.for_ListObserversCommand.when_validating_type;

public class and_type_is_case_insensitive : Specification
{
    bool _result;
    string _errorMessage;

    void Because() => _result = ListObserversCommand.IsValidType("Reducer", out _errorMessage);

    [Fact] void should_be_valid() => _result.ShouldBeTrue();
    [Fact] void should_have_empty_error_message() => _errorMessage.ShouldEqual(string.Empty);
}
