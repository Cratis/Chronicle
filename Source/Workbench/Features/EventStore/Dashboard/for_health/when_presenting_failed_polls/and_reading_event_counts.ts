// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '@cratis/arc/queries';
import { a_dashboard } from './given/a_dashboard';
import strings from 'Strings';

const texts = strings.eventStore.dashboard;
const stale = 'Stale — latest update failed';

describe('when presenting failed event count polls', () => {
    let context: a_dashboard;

    beforeEach(() => { context = new a_dashboard(); });
    afterEach(() => context.dispose());

    describe('given the first count read rejects and health succeeds', () => {
        beforeEach(async () => {
            context.counts.mockRejectedValue(new Error('private count failure'));
            context.render();
            await context.settle();
        });

        it('should show unavailable for the event count', () => context.cells()[1].should.include(texts.unavailable));
        it('should not invent a zero count', () => context.cells()[1].should.not.include('>0'));
        it('should keep health current', () => context.widget(texts.failures).should.include(texts.allPartitionsHealthy));
        it('should keep the healthy success tone', () => context.widget(texts.failures).should.include('dashboard-widget--success'));
        it('should keep namespace health available', () => context.cells()[2].should.not.include(texts.unavailable));
        it('should not expose the rejection text', () => context.html.should.not.include('private count failure'));
    });

    describe('given a later count read rejects and health succeeds', () => {
        beforeEach(async () => {
            context.render();
            await context.settle();
            context.counts.mockRejectedValue(new Error('private count failure'));
            await context.poll(30_000);
        });

        it('should mark the retained count stale', () => context.cells()[1].should.include(stale));
        it('should retain the event count', () => context.cells()[1].should.include('17'));
        it('should keep the namespace health current', () => context.cells()[2].should.not.include(stale));
        it('should keep the healthy checkmark', () => context.widget(texts.failures).should.include('✓'));

        describe('and the event count recovers', () => {
            beforeEach(async () => {
                context.counts.mockResolvedValue(QueryResult.empty({ totalEvents: 23, eventTypes: 0, namespaces: 1, perEventType: [] }));
                await context.poll(30_000);
            });

            it('should clear the stale count presentation', () => context.cells()[1].should.not.include(stale));
            it('should show the new count', () => context.cells()[1].should.include('23'));
        });
    });
});
