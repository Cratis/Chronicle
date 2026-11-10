// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_EventImport.when_completing;

public class and_waiting_for_the_commit : given.an_event_import
{
    ImportResult _result;

    async Task Because()
    {
        await Import.Add(Events(4));
        _result = await Import.Complete();
    }

    [Fact] void should_return_once_the_import_completed() => _result.State.ShouldEqual(ImportState.Completed);
    [Fact] void should_report_the_committed_events() => _result.EventsCommitted.ShouldEqual(4L);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
