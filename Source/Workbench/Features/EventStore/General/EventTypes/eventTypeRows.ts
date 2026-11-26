// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { EventTypeVisibility } from 'Features/Contracts/Events';
import { type EventTypeDetails } from 'Features/EventTypes';

/** An event type row, with the origin as the text the table shows, filters and sorts on. */
export type EventTypeRow = EventTypeDetails & {
    originText: string;
};

export const getEventTypeVisibilityAsText = (visibility: EventTypeVisibility) => {
    const visibilities = strings.eventStore.general.eventTypes.visibilities;
    switch (visibility) {
        case EventTypeVisibility.public:
            return visibilities.public;
        case EventTypeVisibility.private:
            return visibilities.private;
        default:
            return visibilities.unspecified;
    }
};

/** An empty origin is not proof of local ownership, so it reads as not specified. */
export const getEventTypeOriginAsText = (origin: string | undefined) =>
    origin ? origin : strings.eventStore.general.eventTypes.origins.notSpecified;

export const toEventTypeRow = (eventType: EventTypeDetails): EventTypeRow => ({
    ...eventType,
    originText: getEventTypeOriginAsText(eventType.origin)
});

export const eventTypeVisibilityFilterOptions = [
    EventTypeVisibility.public,
    EventTypeVisibility.private,
    EventTypeVisibility.unspecified
].map(visibility => ({ label: getEventTypeVisibilityAsText(visibility), value: visibility }));

/** The distinct origins present in the rows, sorted, for the origin column filter. */
export const getEventTypeOriginFilterOptions = (rows: EventTypeRow[]) =>
    [...new Set(rows.map(row => row.originText))]
        .sort((first, second) => first.localeCompare(second))
        .map(originText => ({ label: originText, value: originText }));
