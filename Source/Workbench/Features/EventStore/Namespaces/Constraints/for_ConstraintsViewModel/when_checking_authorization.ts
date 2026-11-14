// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

for (const [name, authorization, available] of [
    ['authorized', { isReady: true, isSuccess: true, isAuthorized: true, data: { canReindex: true } }, true],
    ['unauthorized', { isReady: true, isSuccess: true, isAuthorized: false, data: { canReindex: true } }, false],
    ['loading', { isReady: false, isSuccess: true, isAuthorized: true, data: { canReindex: true } }, false],
    ['failed', { isReady: true, isSuccess: false, isAuthorized: true, data: { canReindex: true } }, false],
    ['unavailable', { isReady: true, isSuccess: true, isAuthorized: true, data: { canReindex: false } }, false]
] as const) {
    describe(`when checking authorization and ${name}`, given(a_view_model, context => {
        it('should only offer the action after authorization succeeds', () => context.viewModel.canReindex(authorization).should.equal(available));
    }));
}
