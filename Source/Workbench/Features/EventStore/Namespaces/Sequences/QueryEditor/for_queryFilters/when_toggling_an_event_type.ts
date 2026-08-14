// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createSequenceQueryState } from '../SequenceQueryState';
import { toggleEventType } from '../queryFilters';

const state = createSequenceQueryState('the-id', 'The query', 'default');

describe('when toggling an event type that is not selected', () => {
    const result = toggleEventType(state, 'Registered');

    it('should add it to the narrowing', () => result.eventTypes.should.eql(['Registered']));
    it('should not change the original state', () => state.eventTypes.length.should.equal(0));
});

describe('when toggling an event type that is already selected', () => {
    const result = toggleEventType({ ...state, eventTypes: ['Registered', 'Archived'] }, 'Registered');

    it('should remove it from the narrowing', () => result.eventTypes.should.eql(['Archived']));
});
