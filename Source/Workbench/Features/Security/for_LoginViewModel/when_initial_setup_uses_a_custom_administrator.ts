// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { QueryResult } from '@cratis/arc/queries';
import { ChangeUserPassword } from '../ChangeUserPassword';
import { GetStatus } from '../GetStatus';
import { AdminPasswordStatus } from '../AdminPasswordStatus';
import { SetInitialAdminPassword } from '../SetInitialAdminPassword';
import { LoginViewModel } from '../LoginViewModel';

describe('when initial setup uses a custom administrator', () => {
    let viewModel: LoginViewModel;

    beforeEach(async () => {
        const getStatus = sinon.createStubInstance(GetStatus);
        const status = Object.assign(new AdminPasswordStatus(), {
            isRequired: true,
            adminUsername: 'chronicle-root',
        });
        getStatus.perform.resolves(QueryResult.empty(status));
        viewModel = new LoginViewModel(
            sinon.createStubInstance(ChangeUserPassword),
            sinon.createStubInstance(SetInitialAdminPassword),
            getStatus,
        );
        await viewModel.checkInitialSetup();
    });

    it('should use the configured username', () => viewModel.username.should.equal('chronicle-root'));
    it('should require initial setup', () => viewModel.isInitialSetup.should.be.true);
});
