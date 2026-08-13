// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { areSequenceQueryStatesEqual, createSequenceQueryState } from '../SequenceQueryState';

const state = createSequenceQueryState('the-id', 'The query', 'default');

/**
 * Queries save themselves as the user edits, so this comparison is what stops an unchanged query
 * from being written back on every render.
 */
describe('when comparing two query states', () => {
    it('should treat identical states as equal', () =>
        areSequenceQueryStatesEqual(state, { ...state }).should.be.true);

    it('should treat a renamed query as different', () =>
        areSequenceQueryStatesEqual(state, { ...state, name: 'Renamed' }).should.be.false);

    it('should treat a different ordering as different', () =>
        areSequenceQueryStatesEqual(state, { ...state, descending: !state.descending }).should.be.false);

    it('should treat a different time bound as different', () =>
        areSequenceQueryStatesEqual(state, { ...state, occurredFrom: 1 }).should.be.false);

    it('should treat the same event types in a different order as equal', () =>
        areSequenceQueryStatesEqual(
            { ...state, eventTypes: ['Registered', 'Archived'] },
            { ...state, eventTypes: ['Archived', 'Registered'] }).should.be.true);

    it('should treat a different set of event types as different', () =>
        areSequenceQueryStatesEqual(
            { ...state, eventTypes: ['Registered'] },
            { ...state, eventTypes: ['Archived'] }).should.be.false);

    it('should treat a longer set of event types as different', () =>
        areSequenceQueryStatesEqual(
            { ...state, eventTypes: ['Registered'] },
            { ...state, eventTypes: ['Registered', 'Archived'] }).should.be.false);
});
