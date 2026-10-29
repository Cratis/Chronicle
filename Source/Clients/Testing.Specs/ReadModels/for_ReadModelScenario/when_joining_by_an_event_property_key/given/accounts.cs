// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_by_an_event_property_key.given;

public class accounts : Specification
{
    protected ReadModelScenario<AccountSummary> _scenario;
    protected EventSourceId _accountId;
    protected EventSourceId _decoyAccountId;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<AccountSummary>();
        _accountId = EventSourceId.New();
        _decoyAccountId = EventSourceId.New();
        await _scenario.Given.ForEventSource(_accountId).Events(new AccountOpened("c-1"));
        await _scenario.Given.ForEventSource(_decoyAccountId).Events(new AccountOpened("decoy"));
    }
}
