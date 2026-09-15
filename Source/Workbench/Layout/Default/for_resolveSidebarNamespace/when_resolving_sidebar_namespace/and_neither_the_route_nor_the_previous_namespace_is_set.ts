// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveSidebarNamespace } from '../../resolveSidebarNamespace';

describe('when resolving sidebar namespace and neither the route nor the previous namespace is set', () => {
    let result: string;

    beforeEach(() => {
        result = resolveSidebarNamespace(undefined, '');
    });

    it('should return an empty namespace', () => {
        result.should.equal('');
    });
});
