// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandTrackerImplementation } from '../CommandTrackerImplementation';
import { FakeCommand } from './FakeCommand';

describe('when one of the added commands has changes', () => {
    const tracker = new CommandTrackerImplementation(() => {});

    const firstCommand = new FakeCommand(false);
    const secondCommand = new FakeCommand(true);
    const thirdCommand = new FakeCommand(false);

    tracker.addCommand(firstCommand);
    tracker.addCommand(secondCommand);
    tracker.addCommand(thirdCommand);

    it('should have changes', () => tracker.hasChanges.should.be.true);
});
