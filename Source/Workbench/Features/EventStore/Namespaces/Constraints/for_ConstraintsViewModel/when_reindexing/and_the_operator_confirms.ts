// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogButtons } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import strings from 'Strings';
import { a_view_model } from '../given/a_view_model';

const messages = strings.eventStore.namespaces.constraints;

describe('when reindexing and the operator confirms', given(a_view_model, context => {
    beforeEach(async () => { await context.request(); });

    it('should warn that governed appends may wait', () => context.dialogs.showConfirmation.should.have.been.calledWith(
        messages.dialog.title, messages.dialog.message, DialogButtons.YesNo));
    it('should explain the append impact', () => messages.dialog.message.should.contain('governed appends may wait'));
    it('should execute once', () => context.execute.should.have.been.calledOnce);
    it('should target the event store', () => context.command.eventStore.should.equal('eventStore'));
    it('should target the namespace', () => context.command.namespace.should.equal('namespace'));
    it('should target the event log', () => context.command.eventSequenceId.should.equal('event-log'));
    it('should select all applicable constraints', () => context.command.constraintNames.should.deep.equal([]));
    it('should capture the job id', () => context.viewModel.jobId!.should.equal(context.jobId.toString()));
    it('should report acceptance', () => context.viewModel.feedback.should.equal(messages.started));
    it('should release the pending state', () => context.viewModel.isReindexing.should.be.false);
}));
