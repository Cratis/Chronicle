// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_namespaces_store } from '../given/a_namespaces_store';

describe('when namespaces are pushed and there is no current namespace', given(a_namespaces_store, (context) => {
    beforeEach(() => {
        context.store.setRouteNamespace('tenant-a');
        context.store.setEventStore('the-store');
        context.pushNamespaces('Default', 'tenant-a');
    });

    it('should settle on the namespace the route names', () => context.store.currentNamespace.value.should.equal('tenant-a'));
}));
