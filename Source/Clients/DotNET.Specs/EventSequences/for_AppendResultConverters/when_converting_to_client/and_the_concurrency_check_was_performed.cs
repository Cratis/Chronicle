// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters.when_converting_to_client;

/// <summary>
/// Whether the check ran is something only the kernel knows, so the client cannot re-derive it from the scope it
/// sent without risking a different answer than the server gave. It travels on the response, and both append shapes
/// carry it onto <see cref="IAppendResult"/>.
/// </summary>
public class and_the_concurrency_check_was_performed : Specification
{
    IAppendResult _appendResult;
    IAppendResult _appendManyResult;

    void Because()
    {
        _appendResult = new AppendResponse { ConcurrencyCheckPerformed = true }.ToClient();
        _appendManyResult = new AppendManyResponse { ConcurrencyCheckPerformed = true }.ToClient();
    }

    [Fact] void should_report_it_on_the_append_result() => _appendResult.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] void should_report_it_on_the_append_many_result() => _appendManyResult.ConcurrencyCheckPerformed.ShouldBeTrue();
}
