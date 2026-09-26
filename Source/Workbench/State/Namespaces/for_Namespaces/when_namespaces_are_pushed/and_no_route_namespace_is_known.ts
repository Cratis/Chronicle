// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_namespaces_store } from '../given/a_namespaces_store';

describe('when namespaces are pushed and no route namespace is known', given(a_namespaces_store, (context) => {
    beforeEach(() => {
        context.storedNamespace = 'tenant-b';
        context.store.setEventStore('the-store');
        context.pushNamespaces('Default', 'tenant-b');
    });

    it('should fall back to the namespace the user last picked', () => context.store.currentNamespace.value.should.equal('tenant-b'));
}));
