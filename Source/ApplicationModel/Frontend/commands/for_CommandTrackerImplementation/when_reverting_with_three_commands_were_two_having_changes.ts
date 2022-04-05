// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SinonStub } from 'sinon';
import { CommandTrackerImplementation } from '../CommandTrackerImplementation';
import { FakeCommand } from './FakeCommand';

describe('when reverting with three commands were two having changes', () => {
    const tracker = new CommandTrackerImplementation(() => {});

    const firstCommand = new FakeCommand(true);
    const secondCommand = new FakeCommand(false);
    const thirdCommand = new FakeCommand(true);

    tracker.addCommand(firstCommand);
    tracker.addCommand(secondCommand);
    tracker.addCommand(thirdCommand);

    tracker.revertChanges();

    it('should call revert on first command', () => (firstCommand.revertChanges as SinonStub).called.should.be.true);
    it('should call revert on second command', () => (secondCommand.revertChanges as SinonStub).called.should.be.true);
    it('should call revert on third command', () => (thirdCommand.revertChanges as SinonStub).called.should.be.true);
    it('should not have any changes', () => tracker.hasChanges.should.be.false);
});
