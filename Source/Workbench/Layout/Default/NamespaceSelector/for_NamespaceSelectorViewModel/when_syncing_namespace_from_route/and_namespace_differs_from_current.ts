// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when syncing namespace from route and namespace differs from current', given(a_view_model, (context) => {
    beforeEach(() => context.viewModel.syncNamespaceFromRoute('other-namespace'));

    it('should update the current namespace on the namespace store', () => context.setCurrentNamespace.should.be.calledWith('other-namespace'));
}));
