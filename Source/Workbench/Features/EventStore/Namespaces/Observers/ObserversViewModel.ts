// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { ObserverInformation } from 'Features/Observation';
import { ClearObserverQuarantine, RemoveObserver, ReplayObserver as Replay } from 'Features/Observation';
import { IDialogs } from '@cratis/arc.react.mvvm/dialogs';
import { DialogButtons, DialogResult } from '@cratis/arc.react/dialogs';
import { ObserverRunningState } from 'Features/Contracts/Observation';
import { ObserverRemovalOutcome, ObserverRemovalResult } from 'Features/Concepts/Observation';
import strings from 'Strings';

const dialogStrings = strings.eventStore.namespaces.observers.dialogs.removeObserver;

@injectable()
export class ObserversViewModel {

    constructor(
        private readonly _replay: Replay,
        private readonly _clearObserverQuarantine: ClearObserverQuarantine,
        private readonly _removeObserver: RemoveObserver,
        private readonly _dialogs: IDialogs) {
    }

    selectedObserver: ObserverInformation | undefined;
    get canClearObserverQuarantine() {
        return this.selectedObserver?.runningState === ObserverRunningState.quarantined;
    }

    /**
     * Whether the selected observer is one that may be removed.
     *
     * The kernel is the authority here and refuses a running or still-subscribed observer whatever the Workbench
     * thinks. This only keeps the action from being offered for the one case the listing can already rule out, so an
     * operator is not invited to attempt something that is certain to be refused.
     */
    get canRemoveObserver() {
        const runningState = this.selectedObserver?.runningState;
        return runningState !== undefined && runningState !== ObserverRunningState.active;
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

    /**
     * Remove the selected observer after confirming what that costs.
     *
     * Removal is irreversible and reaches every namespace in the event store, so the confirmation spells out what
     * goes and what stays rather than asking a bare "are you sure". A refusal from the kernel is reported as its own
     * message, because "nothing happened" and "it worked" look identical in a listing that refreshes either way.
     */
    async removeObserver(eventStore: string, namespace: string) {
        if (!this.canRemoveObserver || !this.selectedObserver) {
            return;
        }

        const observerId = this.selectedObserver.id;
        const confirmation = await this._dialogs.showConfirmation(
            dialogStrings.title,
            dialogStrings.message.replace('{observerId}', observerId),
            DialogButtons.YesNo);

        if (confirmation !== DialogResult.Yes) {
            return;
        }

        this._removeObserver.eventStore = eventStore;
        this._removeObserver.namespace = namespace;
        this._removeObserver.observerId = observerId;
        this._removeObserver.eventSequenceId = this.selectedObserver.eventSequenceId;

        const commandResult = await this._removeObserver.execute();
        commandResult.onException((error) => {
            this._dialogs.showConfirmation(
                dialogStrings.refusedTitle,
                dialogStrings.failed.replace('{observerId}', observerId).replace('{error}', `${error}`),
                DialogButtons.Ok);
        });

        if (!commandResult.isSuccess) {
            return;
        }

        this.reportOutcome(commandResult.response, observerId, namespace);
    }

    /**
     * Tell the operator what the kernel decided.
     *
     * A refusal is not an error - the command succeeded, it just declined - so nothing surfaces it unless this does,
     * and a listing that refreshes either way looks identical whether the observer went or stayed.
     */
    reportOutcome(result: ObserverRemovalResult | undefined, observerId: string, namespace: string) {
        const message = this.getRefusalMessage(result?.outcome);
        if (!message) {
            this.selectedObserver = undefined;
            return;
        }

        this._dialogs.showConfirmation(
            dialogStrings.refusedTitle,
            message
                .replace('{observerId}', observerId)
                .replace('{namespace}', result?.blockingNamespace || namespace),
            DialogButtons.Ok);
    }

    getRefusalMessage(outcome: ObserverRemovalOutcome | undefined): string | undefined {
        switch (outcome) {
            case ObserverRemovalOutcome.observerActive:
                return dialogStrings.refusedBecauseActive;
            case ObserverRemovalOutcome.observerSubscribed:
                return dialogStrings.refusedBecauseSubscribed;
            case ObserverRemovalOutcome.observerNotFound:
                return dialogStrings.refusedBecauseNotFound;
            default:
                return undefined;
        }
    }
}
