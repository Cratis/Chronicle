// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SinonStub } from 'sinon';
import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeCommand } from './FakeCommand';

describe('when reverting with three commands were two having changes', () => {
    const scope = new CommandScopeImplementation(() => {});

    const firstCommand = new FakeCommand(true);
    const secondCommand = new FakeCommand(false);
    const thirdCommand = new FakeCommand(true);

    scope.addCommand(firstCommand);
    scope.addCommand(secondCommand);
    scope.addCommand(thirdCommand);

    scope.revertChanges();

    it('should call revert on first command', () => (firstCommand.revertChanges as SinonStub).called.should.be.true);
    it('should call revert on second command', () => (secondCommand.revertChanges as SinonStub).called.should.be.true);
    it('should call revert on third command', () => (thirdCommand.revertChanges as SinonStub).called.should.be.true);
    it('should not have any changes', () => scope.hasChanges.should.be.false);
});
