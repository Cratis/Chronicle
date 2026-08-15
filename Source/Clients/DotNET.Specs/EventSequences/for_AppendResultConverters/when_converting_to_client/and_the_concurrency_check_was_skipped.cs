// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters.when_converting_to_client;

/// <summary>
/// The direction that matters most, because a skipped check produces the same successful append as a passing one.
/// A caller has to be able to assert on the difference in a test rather than discover it in production.
/// </summary>
public class and_the_concurrency_check_was_skipped : Specification
{
    IAppendResult _appendResult;
    IAppendResult _appendManyResult;

    void Because()
    {
        _appendResult = new AppendResponse { ConcurrencyCheckPerformed = false }.ToClient();
        _appendManyResult = new AppendManyResponse { ConcurrencyCheckPerformed = false }.ToClient();
    }

    [Fact] void should_report_it_on_the_append_result() => _appendResult.ConcurrencyCheckPerformed.ShouldBeFalse();
    [Fact] void should_report_it_on_the_append_many_result() => _appendManyResult.ConcurrencyCheckPerformed.ShouldBeFalse();
    [Fact] void should_not_make_the_append_look_failed() => _appendResult.IsSuccess.ShouldBeTrue();
}
