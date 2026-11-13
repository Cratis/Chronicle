// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { CommandResult } from '@cratis/arc/commands';
import { QueryResult } from '@cratis/arc/queries';
import { DialogButtons, DialogResult } from '@cratis/arc.react/dialogs';
import { IDialogs } from '@cratis/arc.react.mvvm/dialogs';
import { Guid } from '@cratis/fundamentals';
import { ReindexConstraints } from 'Features/Events/Constraints/Operator/ReindexConstraints';
import strings from 'Strings';

const messages = strings.eventStore.namespaces.constraints;

@injectable()
export class ConstraintsViewModel {
    jobId?: string;
    feedback = '';
    isReindexing = false;

    constructor(private readonly _reindex: ReindexConstraints, private readonly _dialogs: IDialogs) {
    }

    canReindex(authorization: Pick<QueryResult<boolean>, 'isReady' | 'isSuccess' | 'isAuthorized' | 'data'>): boolean {
        return authorization.isReady && authorization.isSuccess && authorization.isAuthorized && authorization.data === true;
    }

    async reindex(eventStore: string, namespace: string) {
        if (this.isReindexing) return;

        this.isReindexing = true;
        this.jobId = undefined;
        this.feedback = '';
        try {
            const confirmation = await this._dialogs.showConfirmation(messages.dialog.title, messages.dialog.message, DialogButtons.YesNo);
            if (confirmation !== DialogResult.Yes) return;

            this._reindex.eventStore = eventStore;
            this._reindex.namespace = namespace;
            this._reindex.eventSequenceId = 'event-log';
            this._reindex.constraintNames = [];
            this.reportResult(await this._reindex.execute());
        } catch (error) {
            this.feedback = error instanceof Error ? error.message : messages.failed;
        } finally {
            this.isReindexing = false;
        }
    }

    private reportResult(result: CommandResult<Guid>) {
        const failure = this.failureMessage(result);
        if (failure) {
            this.feedback = failure;
            return;
        }
        const jobId = result.response;
        if (!result.isSuccess || !jobId || jobId.equals(Guid.empty)) {
            this.feedback = messages.failed;
            return;
        }
        this.jobId = jobId.toString();
        this.feedback = messages.started;
    }

    private failureMessage(result: CommandResult<Guid>): string | undefined {
        if (!result.isAuthorized) return messages.unauthorized;
        if (!result.isValid) return result.validationResults.map(validation => validation.message).join(' ') || messages.invalid;
        if (result.hasExceptions) return result.exceptionMessages.join(' ') || messages.failed;
        return undefined;
    }
}
