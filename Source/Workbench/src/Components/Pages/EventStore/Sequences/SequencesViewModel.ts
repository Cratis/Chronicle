// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { observable, makeAutoObservable } from 'mobx';
import { AppendedEventWithJsonAsContent } from 'API/events/store/sequence/AppendedEventWithJsonAsContent';
import { SequencesTestData } from './SequencesTestData';

export class SequencesViewModel {

    constructor() {
        makeAutoObservable(this);

        /*
        Filtering:

        - eventType(s) -> event.metadata.type.id
        - eventSourceId -> event.context.eventSourceId
        - time range (from : to) -> event.context.occurred (we can probably wait with this till we have the data zoom from eCharts)

        https://github.com/orgs/Cratis/projects/1?pane=issue&itemId=41367866
        */
    }

    @observable events: AppendedEventWithJsonAsContent[] = SequencesTestData.GetEvents(10);
}
