// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when syncing namespace from route and namespace differs from current route namespace', given(a_view_model, (context) => {
    beforeEach(() => context.viewModel.syncNamespaceFromRoute('other-namespace'));

    it('should tell the namespace store which namespace the route names', () => context.setRouteNamespace.should.be.calledWith('other-namespace'));
}));
