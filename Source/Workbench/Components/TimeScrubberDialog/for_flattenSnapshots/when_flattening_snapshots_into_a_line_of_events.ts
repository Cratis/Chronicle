// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReadModelSnapshot } from 'Features/ReadModelExplorer';
import { flattenSnapshots } from '../flattenSnapshots';

const snapshot = (occurred: string, instance: object, ...types: string[]) => ({
    occurred: new Date(occurred),
    instance: JSON.stringify(instance),
    events: types.map((type, index) => ({
        context: { sequenceNumber: index, eventType: { id: type }, occurred: new Date(occurred) },
        content: '{}'
    }))
} as unknown as ReadModelSnapshot);

describe('when flattening snapshots into a line of events', () => {
    it('should produce nothing for no snapshots', () => flattenSnapshots([]).length.should.equal(0));

    it('should produce one step per event', () =>
        flattenSnapshots([
            snapshot('2026-01-01', { total: 1 }, 'Opened', 'Credited'),
            snapshot('2026-01-02', { total: 2 }, 'Debited')
        ]).length.should.equal(3));

    it('should keep the events in the order the snapshots hold them', () =>
        flattenSnapshots([
            snapshot('2026-01-01', { total: 1 }, 'Opened', 'Credited'),
            snapshot('2026-01-02', { total: 2 }, 'Debited')
        ]).map(step => step.event.context.eventType.id).should.eql(['Opened', 'Credited', 'Debited']));

    it('should give every event of a snapshot that snapshot\'s state', () => {
        const steps = flattenSnapshots([snapshot('2026-01-01', { total: 7 }, 'Opened', 'Credited')]);
        steps.map(step => step.instance).should.eql([{ total: 7 }, { total: 7 }]);
    });

    it('should carry the state of the snapshot each event belongs to', () => {
        const steps = flattenSnapshots([
            snapshot('2026-01-01', { total: 1 }, 'Opened'),
            snapshot('2026-01-02', { total: 2 }, 'Debited')
        ]);
        steps.map(step => step.instance).should.eql([{ total: 1 }, { total: 2 }]);
    });

    it('should carry when the snapshot was taken', () =>
        flattenSnapshots([snapshot('2026-01-01', {}, 'Opened')])[0]
            .occurred.should.eql(new Date('2026-01-01')));

    it('should skip a snapshot that holds no events', () =>
        flattenSnapshots([
            snapshot('2026-01-01', { total: 1 }),
            snapshot('2026-01-02', { total: 2 }, 'Debited')
        ]).map(step => step.event.context.eventType.id).should.eql(['Debited']));

    it('should survive a snapshot with no events at all', () =>
        flattenSnapshots([{ occurred: new Date(), instance: '{}' } as unknown as ReadModelSnapshot])
            .length.should.equal(0));
});
