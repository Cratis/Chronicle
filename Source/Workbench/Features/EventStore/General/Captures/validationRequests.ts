// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CommandResult } from '@cratis/arc/commands';
import type { CaptureValidationMessage } from 'Features/Contracts/Captures';

/** How long the declaration has to rest before it is validated by the server. */
export const validationDelayMs = 500;

/** What a validation request is made for. */
export interface ValidationSnapshot {
    readonly eventStore: string;
    readonly declaration: string;
}

/** The part of the validate command the requests depend on. */
export interface ValidationCommand {
    eventStore: string;
    declaration: string;
    execute(): Promise<CommandResult<{ messages: CaptureValidationMessage[] }>>;
}

/**
 * Validates a snapshot with the command. The values are set and the request is dispatched in the same
 * synchronous step, so a later snapshot can never leak into this request through the shared command.
 * @param command The command to execute.
 * @param snapshot The snapshot to validate.
 * @returns The validation messages, or null when the request did not succeed.
 */
export const validateSnapshot = async (command: ValidationCommand, snapshot: ValidationSnapshot): Promise<CaptureValidationMessage[] | null> => {
    command.eventStore = snapshot.eventStore;
    command.declaration = snapshot.declaration;
    const result = await command.execute();
    return result.isSuccess ? result.response?.messages ?? [] : null;
};

/**
 * Debounces capture validation and only lets the response of the latest snapshot reach the screen.
 */
export class ValidationRequests {
    #revision = 0;
    #timer: ReturnType<typeof setTimeout> | undefined;

    constructor(
        private readonly _execute: (snapshot: ValidationSnapshot) => Promise<CaptureValidationMessage[] | null>,
        private readonly _publish: (messages: CaptureValidationMessage[]) => void,
        private readonly _delayMs: number = validationDelayMs) {
    }

    /**
     * Supersedes everything requested so far and validates the snapshot once the delay has passed.
     * A blank declaration has nothing to validate and clears the messages.
     * @param snapshot The snapshot to validate.
     */
    schedule(snapshot: ValidationSnapshot): void {
        this.cancel();
        if (!snapshot.declaration.trim()) {
            this._publish([]);
            return;
        }

        const revision = this.#revision;
        this.#timer = setTimeout(() => {
            this.#timer = undefined;
            void this.#dispatch(snapshot, revision);
        }, this._delayMs);
    }

    /** Supersedes everything requested so far: queued work never runs and running work is ignored. */
    cancel(): void {
        this.#revision++;
        if (this.#timer !== undefined) {
            clearTimeout(this.#timer);
            this.#timer = undefined;
        }
    }

    async #dispatch(snapshot: ValidationSnapshot, revision: number): Promise<void> {
        try {
            const messages = await this._execute(snapshot);
            if (messages && revision === this.#revision) {
                this._publish(messages);
            }
        } catch {
            // A failed request leaves the messages as they were; there is nothing to report about a declaration the server never judged.
        }
    }
}
