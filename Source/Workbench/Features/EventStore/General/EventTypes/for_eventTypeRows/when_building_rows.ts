// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventType, EventTypeVisibility } from 'Features/Contracts/Events';
import { EventTypeDetails } from 'Features/EventTypes';
import {
    eventTypeVisibilityFilterOptions,
    getEventTypeOriginFilterOptions,
    getEventTypeVisibilityAsText,
    toEventTypeRow
} from '../eventTypeRows';

const eventTypeWith = (id: string, visibility: EventTypeVisibility, origin: string): EventTypeDetails => {
    const eventType = new EventType();
    eventType.id = id;
    eventType.generation = 1;

    const details = new EventTypeDetails();
    details.id = id;
    details.type = eventType;
    details.visibility = visibility;
    details.origin = origin;
    return details;
};

describe('when presenting visibility', () => {
    it('should name public', () => getEventTypeVisibilityAsText(EventTypeVisibility.public).should.equal('Public'));
    it('should name private', () => getEventTypeVisibilityAsText(EventTypeVisibility.private).should.equal('Private'));
    it('should name unspecified as legacy, including enum zero', () => getEventTypeVisibilityAsText(EventTypeVisibility.unspecified).should.equal('Unspecified (legacy)'));
    it('should offer every visibility as a filter option', () => eventTypeVisibilityFilterOptions.map(option => option.value).should.have.members([
        EventTypeVisibility.public,
        EventTypeVisibility.private,
        EventTypeVisibility.unspecified
    ]));
});

describe('when building a row with an empty origin', () => {
    const row = toEventTypeRow(eventTypeWith('a', EventTypeVisibility.public, ''));

    it('should show not specified', () => row.originText.should.equal('Not specified'));
    it('should keep the raw origin', () => row.origin.should.equal(''));
    it('should keep the id', () => row.id.should.equal('a'));
});

describe('when building a row with a foreign origin', () => {
    const row = toEventTypeRow(eventTypeWith('a', EventTypeVisibility.public, 'other-store'));

    it('should show the origin', () => row.originText.should.equal('other-store'));
});

describe('when building origin filter options', () => {
    const options = getEventTypeOriginFilterOptions([
        toEventTypeRow(eventTypeWith('a', EventTypeVisibility.public, 'zeta')),
        toEventTypeRow(eventTypeWith('b', EventTypeVisibility.private, '')),
        toEventTypeRow(eventTypeWith('c', EventTypeVisibility.public, 'zeta')),
        toEventTypeRow(eventTypeWith('d', EventTypeVisibility.public, 'alpha'))
    ]);

    it('should list each distinct origin once, sorted', () => options.map(option => option.value).should.deep.equal(['alpha', 'Not specified', 'zeta']));
});
