// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandResult } from '@cratis/arc/commands';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { Dialogs } from '@cratis/arc.react.mvvm/dialogs';
import { Guid } from '@cratis/fundamentals';
import { ReindexConstraints } from 'Features/Events/Constraints/Operator/ReindexConstraints';
import { ConstraintsViewModel } from '../../ConstraintsViewModel';

export class a_view_model {
    readonly command = new ReindexConstraints();
    readonly execute: sinon.SinonStub = sinon.stub(this.command, 'execute');
    readonly dialogs = sinon.createStubInstance(Dialogs);
    readonly jobId = Guid.parse('82c60198-978e-47f7-a636-082c73a90123');
    readonly viewModel = new ConstraintsViewModel(this.command, this.dialogs);

    constructor() {
        beforeEach(() => {
            this.viewModel.jobId = undefined;
            this.viewModel.feedback = '';
            this.viewModel.isReindexing = false;
            this.viewModel.isReindexDenied = false;
            this.dialogs.showConfirmation.reset();
            this.dialogs.showConfirmation.resolves(DialogResult.Yes);
            this.execute.reset();
            this.execute.resolves(new CommandResult({
                ...CommandResult.empty,
                correlationId: Guid.empty.toString(),
                response: this.jobId.toString()
            }, Guid, false));
        });
    }

    request() {
        return this.viewModel.reindex('eventStore', 'namespace');
    }
}
