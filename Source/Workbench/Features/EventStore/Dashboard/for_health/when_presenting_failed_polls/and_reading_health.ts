// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '@cratis/arc/queries';
import { ObserverRunningState } from 'Features/Contracts/Observation';
import { ObserverInformation, FailedPartitionDetails } from 'Features/Observation';
import { a_dashboard } from './given/a_dashboard';
import strings from 'Strings';

const texts = strings.eventStore.dashboard;
const stale = 'Stale — latest update failed';

describe('when presenting health polls', () => {
    let context: a_dashboard;

    beforeEach(() => { context = new a_dashboard(); });
    afterEach(() => context.dispose());

    describe('given the first read has not completed', () => {
        beforeEach(() => {
            context.observers.mockReturnValue(new Promise(() => {}));
            context.render();
        });

        it('should not report a failed read', () => context.html.should.not.include(texts.unavailable));
        it('should leave the observer figure pending', () => context.widget(texts.observers).should.include('dashboard-tile__value">–'));
    });

    describe('given a fresh healthy zero sample', () => {
        beforeEach(async () => {
            context.render();
            await context.settle();
        });

        it('should retain the healthy partition caption', () => context.widget(texts.failures).should.include(texts.allPartitionsHealthy));
        it('should retain the success tone', () => context.widget(texts.failures).should.include('dashboard-widget--success'));
        it('should retain the checkmark', () => context.widget(texts.failures).should.include('✓'));
        it('should show zero observers', () => context.widget(texts.observers).should.include('dashboard-tile__value">0'));

        describe('and a later namespace read rejects', () => {
            beforeEach(async () => {
                context.observers.mockImplementation(async args => {
                    if (args?.namespace === 'second') throw new Error('private server failure');
                    return QueryResult.empty([]);
                });
                await context.poll();
            });

            it('should identify the retained sample as stale', () => context.widget(texts.failures).should.include(stale));
            it('should suppress healthy captions', () => context.html.should.not.include(texts.allPartitionsHealthy));
            it('should suppress success tones', () => context.html.should.not.include('--success'));
            it('should suppress checkmarks', () => context.html.should.not.include('✓'));
            it('should retain the zero figure', () => context.widget(texts.failures).should.include('dashboard-tile__value">0'));
            it('should mark even the successful namespace health stale', () => context.cells('first')[2].should.include(stale));
            it('should mark the rejected namespace health stale', () => context.cells('second')[2].should.include(stale));
            it('should keep event counts current', () => context.cells()[1].should.equal('<td><span>17</span></td>'));
            it('should not expose the rejection text', () => context.html.should.not.include('private server failure'));

            describe('and the next read succeeds', () => {
                beforeEach(async () => {
                    context.observers.mockResolvedValue(QueryResult.empty([]));
                    await context.poll();
                });

                it('should clear stale presentation', () => context.html.should.not.include(stale));
                it('should restore healthy captions', () => context.widget(texts.failures).should.include(texts.allPartitionsHealthy));
                it('should restore success tones', () => context.widget(texts.failures).should.include('dashboard-widget--success'));
                it('should restore checkmarks', () => context.widget(texts.failures).should.include('✓'));
            });
        });
    });

    describe('given the first combined health read rejects', () => {
        beforeEach(async () => {
            context.observers.mockImplementation(async args => {
                if (args?.namespace === 'second') throw new Error('private server failure');
                return QueryResult.empty([]);
            });
            context.render();
            await context.settle();
        });

        it('should report unavailable instead of waiting forever', () => context.widget(texts.failures).should.include(texts.unavailable));
        it('should not assert healthy partitions', () => context.html.should.not.include(texts.allPartitionsHealthy));
        it('should not show invented observer zeros', () => context.widget(texts.observers).should.not.include('dashboard-stat__value">0'));
        it('should report unavailable for the successful namespace too', () => context.cells('first')[2].should.include(texts.unavailable));
        it('should keep the successful event count available', () => context.cells()[1].should.include('17'));
        it('should not expose the rejection text', () => context.html.should.not.include('private server failure'));

        describe('and health recovers', () => {
            beforeEach(async () => {
                context.observers.mockResolvedValue(QueryResult.empty([]));
                await context.poll();
            });

            it('should clear unavailable presentation', () => context.html.should.not.include(texts.unavailable));
            it('should restore the current healthy state', () => context.widget(texts.failures).should.include('dashboard-widget--success'));
        });
    });

    describe('given retained unhealthy figures after a rejection', () => {
        beforeEach(async () => {
            context.observers.mockResolvedValue(QueryResult.empty([
                { runningState: ObserverRunningState.disconnected, nextEventSequenceNumber: 1, tailEventSequenceNumber: 2 } as ObserverInformation
            ]));
            context.partitions.mockResolvedValue(QueryResult.empty([
                { isResolved: false }, { isResolved: false }, { isResolved: false }
            ] as FailedPartitionDetails[]));
            context.render();
            await context.settle();
            context.observers.mockRejectedValue(new Error('private server failure'));
            await context.poll();
        });

        it('should retain the combined failed partition count', () => context.widget(texts.failures).should.include('dashboard-tile__value">6'));
        it('should identify retained failures as stale', () => context.widget(texts.failures).should.include(stale));
        it('should retain the stopped observers in the namespace row', () => context.cells()[2].should.include(`(1 ${texts.stopped})`));
        it('should retain the failed partition count in each namespace', () => context.cells()[5].should.include('3'));
    });
});
