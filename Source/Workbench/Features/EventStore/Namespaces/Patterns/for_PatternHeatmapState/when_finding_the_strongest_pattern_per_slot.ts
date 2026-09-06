// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorPattern } from 'Api/Patterns/BehaviorPattern';
import { strongestBySlot } from '../PatternHeatmapState';
import { aPattern } from './given/a_pattern';

describe('when finding the strongest pattern per slot', () => {
    let weak: BehaviorPattern;
    let strong: BehaviorPattern;
    let elsewhere: BehaviorPattern;
    let withoutASlot: BehaviorPattern;
    let result: Map<string, BehaviorPattern>;

    beforeEach(() => {
        weak = aPattern({ Day: 'Monday', TimeBucket: 'Morning', CommandType: 'RejectExpenseReport' }, 0.4);
        strong = aPattern({ Day: 'Monday', TimeBucket: 'Morning', CommandType: 'ApproveExpenseReport' }, 0.9);
        elsewhere = aPattern({ Day: 'Friday', TimeBucket: 'Evening', CommandType: 'SubmitExpenseReport' }, 0.7);
        withoutASlot = aPattern({ CommandType: 'ApproveExpenseReport' }, 1);
        result = strongestBySlot([weak, strong, elsewhere, withoutASlot]);
    });

    it('should keep one pattern per slot', () => result.size.should.equal(2));
    it('should let the most confident speak for the slot', () => result.get('Monday|Morning')!.should.equal(strong));
    it('should keep a pattern in another slot', () => result.get('Friday|Evening')!.should.equal(elsewhere));
    it('should leave out a pattern that belongs to no slot', () => [...result.values()].includes(withoutASlot).should.be.false);
});
