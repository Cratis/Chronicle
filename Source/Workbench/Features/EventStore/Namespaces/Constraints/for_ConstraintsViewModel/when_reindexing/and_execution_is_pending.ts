// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '@cratis/arc/commands';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import { a_view_model } from '../given/a_view_model';

describe('when reindexing and execution is pending', given(a_view_model, context => {
    let wasPending: boolean;

    beforeEach(async () => {
        let complete!: (result: CommandResult) => void;
        let started!: () => void;
        const executing = new Promise<void>(resolve => { started = resolve; });
        context.execute.callsFake(() => {
            started();
            return new Promise<CommandResult>(resolve => { complete = resolve; });
        });
        const request = context.request();
        await executing;
        wasPending = context.viewModel.isReindexing;
        await context.request();
        complete(CommandResult.empty);
        await request;
    });

    it('should hold the pending state', () => wasPending.should.be.true);
    it('should confirm once', () => context.dialogs.showConfirmation.should.have.been.calledOnce);
    it('should execute once', () => context.execute.should.have.been.calledOnce);
    it('should release the pending state', () => context.viewModel.isReindexing.should.be.false);
}));

describe('when reindexing and confirmation is pending', given(a_view_model, context => {
    beforeEach(async () => {
        let confirm!: (result: DialogResult) => void;
        context.dialogs.showConfirmation.callsFake(() => new Promise<DialogResult>(resolve => { confirm = resolve; }));
        const request = context.request();
        await context.request();
        confirm(DialogResult.Yes);
        await request;
    });

    it('should confirm once', () => context.dialogs.showConfirmation.should.have.been.calledOnce);
    it('should execute once', () => context.execute.should.have.been.calledOnce);
}));
