// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandTrackerImplementation } from '../CommandTrackerImplementation';

describe('when there are no commands', () => {
    const tracker = new CommandTrackerImplementation(() => {});

    it('should have no changes', () => tracker.hasChanges.should.be.false);
});
