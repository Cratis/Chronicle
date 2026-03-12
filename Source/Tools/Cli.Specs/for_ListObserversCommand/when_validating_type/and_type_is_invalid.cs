// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli.Commands.Observers;

namespace Cratis.Chronicle.Cli.for_ListObserversCommand.when_validating_type;

public class and_type_is_invalid : Specification
{
    bool _result;
    string _errorMessage;

    void Because() => _result = ListObserversCommand.IsValidType("bogus", out _errorMessage);

    [Fact] void should_not_be_valid() => _result.ShouldBeFalse();
    [Fact] void should_include_the_invalid_type_in_error_message() => _errorMessage.ShouldContain("bogus");
}
