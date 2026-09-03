// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { QueryResult } from '@cratis/arc/queries';
import {
    ChangePasswordForUser,
    GetStatus,
    InitialAdminPasswordSetupStatus,
    SetInitialAdminPassword,
} from 'Api/Security';
import { LoginViewModel } from '../LoginViewModel';

describe('when initial setup uses a custom administrator', () => {
    let viewModel: LoginViewModel;

    beforeEach(async () => {
        const getStatus = sinon.createStubInstance(GetStatus);
        const status = Object.assign(new InitialAdminPasswordSetupStatus(), {
            isRequired: true,
            adminUsername: 'chronicle-root',
        });
        getStatus.perform.resolves(QueryResult.empty(status));
        viewModel = new LoginViewModel(
            sinon.createStubInstance(ChangePasswordForUser),
            sinon.createStubInstance(SetInitialAdminPassword),
            getStatus,
        );

        await viewModel.checkInitialSetup();
    });

    it('should use the configured username', () =>
        viewModel.username.should.equal('chronicle-root'));
    it('should require initial setup', () => viewModel.isInitialSetup.should.be.true);
});
