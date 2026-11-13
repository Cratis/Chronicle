// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_EventImport.when_completing;

public class and_the_import_failed : given.an_event_import
{
    ImportResult _result;

    void Establish() => _finalState = ImportState.Failed;

    async Task Because()
    {
        await Import.Add(Events(1));
        _result = await Import.Complete();
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_failed_state() => _result.State.ShouldEqual(ImportState.Failed);
}
