// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandTrackerImplementation } from '../CommandTrackerImplementation';
import { FakeCommand } from './FakeCommand';

describe('when none of the added commands has changes', () => {
    const tracker = new CommandTrackerImplementation(() => {});

    const firstCommand = new FakeCommand(false);
    const secondCommand = new FakeCommand(false);
    const thirdCommand = new FakeCommand(false);

    tracker.addCommand(firstCommand);
    tracker.addCommand(secondCommand);
    tracker.addCommand(thirdCommand);

    it('should not have changes', () => tracker.hasChanges.should.be.false);
});
