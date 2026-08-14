// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { asSaved, hasUnsavedChanges } from '../OpenQuery';
import { createSequenceQueryState } from '../SequenceQueryState';

const state = createSequenceQueryState('the-id', 'The query', 'default');

describe('when the query has never been saved', () => {
    it('should have something to save', () => hasUnsavedChanges({ state, saved: null }).should.be.true);
});

describe('when the query matches what was last written back', () => {
    it('should have nothing to save', () => hasUnsavedChanges(asSaved(state)).should.be.false);
});

describe('when a filter changed since the query was written back', () => {
    const open = asSaved(state);

    it('should have something to save', () =>
        hasUnsavedChanges({ ...open, state: { ...state, eventTypes: ['Registered'] } }).should.be.true);
});

describe('when only the correlation changed since the query was written back', () => {
    const open = asSaved(state);

    it('should have something to save', () =>
        hasUnsavedChanges({ ...open, state: { ...state, correlationId: 'the-correlation' } }).should.be.true);
});
