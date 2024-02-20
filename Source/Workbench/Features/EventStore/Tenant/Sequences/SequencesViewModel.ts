// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


export type Query = {
    name: string;
};


export class SequencesViewModel {

    constructor() {
        /*
            - Top level component that holds the view and view model for the queries / tabs
            - Component for the queryable event sequence

            Generic components:

            - Filters component (Collection of filter component)
            - Filter component


            - Filters component could have :
                 - onFilterAdded -> add the filter to the queryable sequence view model
                 - onFilterChanged -> modify the filter to the queryable sequence view model
        */

        /*
        Filtering:

        - eventType(s) -> event.metadata.type.id
        - eventSourceId -> event.context.eventSourceId
        - time range (from : to) -> event.context.occurred (we can probably wait with this till we have the data zoom from eCharts)

        https://github.com/orgs/Cratis/projects/1?pane=issue&itemId=41367866
        */
    }

    get queries(): Query[] {
        return [
            { name: 'Query 1' },
            { name: 'Query 2' },
            { name: 'Query 3' }
        ];
    }
}
