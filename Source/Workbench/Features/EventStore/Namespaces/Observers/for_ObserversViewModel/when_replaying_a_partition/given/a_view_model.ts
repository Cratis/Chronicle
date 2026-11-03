// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandResult } from '@cratis/arc/commands';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { ObserverInformation } from 'Features/Observation';
import { ObserverOwner } from 'Features/Contracts/Observation';
import { a_view_model as base_view_model } from '../../when_replaying/given/a_view_model';

export class a_view_model extends base_view_model {
    constructor() {
        super();
        this.execute = sinon.stub(this.replayPartition, 'execute');
        beforeEach(() => {
            this.viewModel.selectedObserver = Object.assign(new ObserverInformation(), {
                id: 'selected-observer', eventSequenceId: 'inbox-source', owner: ObserverOwner.client
            });
            this.viewModel.partitionEventSourceId = ' Customer-A/1,Two ';
            this.viewModel.partitionReplayFeedback = '';
            this.viewModel.partitionReplayAccepted = false;
            this.viewModel.isReplayingPartition = false;
            this.dialogs.showConfirmation.reset();
            this.dialogs.showConfirmation.resolves(DialogResult.Yes);
            this.execute.reset();
            this.execute.resolves(CommandResult.empty);
        });
    }

    execute: sinon.SinonStub;

    request() {
        return this.viewModel.replayPartition(this.params.eventStore!, this.params.namespace!);
    }
}
