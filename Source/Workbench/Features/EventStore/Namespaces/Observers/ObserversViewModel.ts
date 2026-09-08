// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { ObserverInformation } from 'Features/Observation';
import { ClearObserverQuarantine, ReplayObserver as Replay } from 'Features/Observation';
import { IDialogs } from '@cratis/arc.react.mvvm/dialogs';
import { DialogButtons, DialogResult } from '@cratis/arc.react/dialogs';
import { ObserverRunningState } from 'Features/Contracts/Observation';

@injectable()
export class ObserversViewModel {

    constructor(
        private readonly _replay: Replay,
        private readonly _clearObserverQuarantine: ClearObserverQuarantine,
        private readonly _dialogs: IDialogs) {
    }

    selectedObserver: ObserverInformation | undefined;
    get canClearObserverQuarantine() {
        return this.selectedObserver?.runningState === ObserverRunningState.quarantined;
    }

    async replay(eventStore: string, namespace: string) {
        if (this.selectedObserver) {
            const observerId = this.selectedObserver.id;
            const result = await this._dialogs.showConfirmation('Replay?', `Are you sure you want to replay ${observerId}?`, DialogButtons.YesNo);
            if (result == DialogResult.Yes) {
                this._replay.eventStore = eventStore;
                this._replay.namespace = namespace;
                this._replay.observerId = observerId;
                const commandResult = await this._replay.execute();
                commandResult
                    .onException((error) => {
                        this._dialogs.showConfirmation('Replay', `Replay ${observerId} failed: ${error}`, DialogButtons.Ok);
                    });
            }
        }
    }

    async clearObserverQuarantine(eventStore: string, namespace: string) {
        if (!this.canClearObserverQuarantine || !this.selectedObserver) {
            return;
        }

        const observerId = this.selectedObserver.id;
        this._clearObserverQuarantine.eventStore = eventStore;
        this._clearObserverQuarantine.namespace = namespace;
        this._clearObserverQuarantine.observerId = observerId;
        const commandResult = await this._clearObserverQuarantine.execute();
        commandResult
            .onException((error) => {
                this._dialogs.showConfirmation('Clear quarantine', `Clear quarantine for ${observerId} failed: ${error}`, DialogButtons.Ok);
            });
    }
}
