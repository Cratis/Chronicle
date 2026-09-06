// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorPattern } from 'Api/Patterns/BehaviorPattern';
import { busiestSlot, intensityOf, slotKey, strongestBySlot } from '../PatternHeatmapState';
import { aPattern } from './given/a_pattern';

describe('when a slot holds patterns of differing activity', () => {
    let busy: BehaviorPattern;
    let quiet: BehaviorPattern;
    let result: Map<string, BehaviorPattern>;

    beforeEach(() => {
        // Both fully confident, which is the normal case: confidence saturates for anything habitual, so it
        // cannot be what separates a slot somebody lives in from one they touch occasionally.
        quiet = aPattern({ Day: 'Monday', TimeBucket: 'Morning', CommandType: 'RejectExpenseReport' }, 1, 12);
        busy = aPattern({ Day: 'Monday', TimeBucket: 'Morning', CommandType: 'ApproveExpenseReport' }, 1, 276);
        result = strongestBySlot([quiet, busy]);
    });

    it('should let the busiest pattern speak for the slot', () => result.get(slotKey({ day: 'Monday', timeBucket: 'Morning' }))!.should.equal(busy));
    it('should scale the shading against the busiest slot', () => busiestSlot(result).should.equal(276));
    it('should give the busiest slot the full intensity', () => intensityOf(busy, 276)!.should.equal(1));
    it('should lift a quieter slot clear of the darkest step', () => intensityOf(quiet, 276)!.should.be.closeTo(Math.sqrt(12 / 276), 0.0001));
    it('should still keep the quieter slot well below the busiest', () => (intensityOf(quiet, 276)! < 0.5).should.be.true);
    it('should give no intensity to a slot holding nothing', () => (intensityOf(undefined, 276) === undefined).should.be.true);
    it('should give no intensity when nothing is established at all', () => (intensityOf(busy, 0) === undefined).should.be.true);
});
