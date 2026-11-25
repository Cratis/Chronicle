// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from '@cratis/components/DataTables';
import strings from 'Strings';
import {
    eventTypeVisibilityFilterOptions,
    getEventTypeVisibilityAsText,
    type EventTypeRow
} from './eventTypeRows';

const renderVisibility = (eventType: EventTypeRow) => getEventTypeVisibilityAsText(eventType.visibility);

/**
 * Builds the visibility column. Returned as an element rather than a component because the table
 * reads its column definitions from the elements it is given.
 * @returns The visibility column element.
 */
export const visibilityColumn = () => (
    <Column
        key='visibility'
        field='visibility'
        style={{ width: '140px' }}
        header={strings.eventStore.general.eventTypes.columns.visibility}
        dataType='numeric'
        showFilterMatchModes={false}
        filter
        filterField='visibility'
        filterOptions={eventTypeVisibilityFilterOptions}
        body={renderVisibility}
        sortable />
);

/**
 * Builds the origin column. The origin is picked from a list of known values, so it must match exactly:
 * the text default (contains) would also show `store-a-remote` when `store-a` is selected. The
 * non-text data type makes equals the default mode, including after the filter has been cleared.
 * @param filterOptions The distinct origins present in the table.
 * @returns The origin column element.
 */
export const originColumn = (filterOptions: { label: string; value: string }[]) => (
    <Column
        key='origin'
        field='originText'
        style={{ width: '160px' }}
        header={strings.eventStore.general.eventTypes.columns.origin}
        dataType='numeric'
        showFilterMatchModes={false}
        filter
        filterField='originText'
        filterOptions={filterOptions}
        sortable />
);
