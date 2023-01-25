// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeCommand } from './FakeCommand';

describe('when none of the added commands has changes', () => {
    const scope = new CommandScopeImplementation(() => {});

    const firstCommand = new FakeCommand(false);
    const secondCommand = new FakeCommand(false);
    const thirdCommand = new FakeCommand(false);

    scope.addCommand(firstCommand);
    scope.addCommand(secondCommand);
    scope.addCommand(thirdCommand);

    it('should not have changes', () => scope.hasChanges.should.be.false);
});
