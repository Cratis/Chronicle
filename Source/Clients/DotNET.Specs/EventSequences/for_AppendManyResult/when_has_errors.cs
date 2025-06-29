// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendManyResult;

public class when_has_errors : Specification
{
    AppendManyResult _result;
    AppendError _error;

    void Establish()
    {
        _error = new AppendError("something went wrong");
        _result = new AppendManyResult
        {
            Errors = [_error]
        };
    }

    [Fact] void should_have_errors() => _result.HasErrors.ShouldBeTrue();
    [Fact] void should_not_be_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_return_the_error() => _result.Errors.ShouldContain(_error);
}
