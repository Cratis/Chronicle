// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandScopeImplementation } from '../CommandScopeImplementation';

describe('when there are no commands', () => {
    const scope = new CommandScopeImplementation(() => {});

    it('should have no changes', () => scope.hasChanges.should.be.false);
});
