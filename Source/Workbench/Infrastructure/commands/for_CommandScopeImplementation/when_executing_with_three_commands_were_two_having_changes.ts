// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SinonStub } from 'sinon';
import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeCommand } from './FakeCommand';

describe('when executing with three commands were two having changes', async () => {
    const scope = new CommandScopeImplementation(() => {});

    const firstCommand = new FakeCommand(true);
    const secondCommand = new FakeCommand(false);
    const thirdCommand = new FakeCommand(true);

    scope.addCommand(firstCommand);
    scope.addCommand(secondCommand);
    scope.addCommand(thirdCommand);

    await scope.execute();

    it('should call execute on first command', () => (firstCommand.execute as SinonStub).called.should.be.true);
    it('should not call execute on second command', () => (secondCommand.execute as SinonStub).called.should.be.false);
    it('should call execute on third command', () => (thirdCommand.execute as SinonStub).called.should.be.true);
    it('should not have any changes', () => scope.hasChanges.should.be.false);
});
