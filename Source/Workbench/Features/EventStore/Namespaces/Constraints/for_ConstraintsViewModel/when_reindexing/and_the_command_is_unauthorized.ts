// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '@cratis/arc/commands';
import { given } from 'given';
import strings from 'Strings';
import { a_view_model } from '../given/a_view_model';

const authorization = { isReady: true, isSuccess: true, isAuthorized: true, data: { canReindex: true } };

describe('when the probe permits reindexing but the command is unauthorized', given(a_view_model, context => {
    let wasAvailable: boolean;

    beforeEach(async () => {
        wasAvailable = context.viewModel.canReindex(authorization);
        context.execute.resolves(new CommandResult({
            ...CommandResult.empty,
            correlationId: CommandResult.empty.correlationId.toString(),
            isSuccess: false,
            isAuthorized: false,
            response: undefined
        }, Object, false));
        await context.request();
        await context.request();
    });

    it('should initially offer the action', () => wasAvailable.should.be.true);
    it('should hide the action despite the successful probe', () => context.viewModel.canReindex(authorization).should.be.false);
    it('should show the localized unauthorized message', () => context.viewModel.feedback.should.equal(strings.eventStore.namespaces.constraints.unauthorized));
    it('should not repeat confirmation', () => context.dialogs.showConfirmation.should.have.been.calledOnce);
    it('should not repeat execution', () => context.execute.should.have.been.calledOnce);
    it('should not offer a job link', () => (context.viewModel.jobId === undefined).should.be.true);
    it('should release the pending state', () => context.viewModel.isReindexing.should.be.false);
}));
