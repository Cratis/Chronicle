// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import type { CaptureValidationMessage } from 'Features/Contracts/Captures';
import { ValidationRequests, validationDelayMs, type ValidationSnapshot } from '../../validationRequests';

export type Deferred = { snapshot: ValidationSnapshot; resolve: (messages: CaptureValidationMessage[] | null) => void; reject: (error: unknown) => void };

export const message = (text: string): CaptureValidationMessage => ({ line: 1, column: 1, message: text }) as CaptureValidationMessage;

export class a_harness {
    readonly clock = sinon.useFakeTimers();
    readonly requests: Deferred[] = [];
    readonly published: CaptureValidationMessage[][] = [];
    readonly validation = new ValidationRequests(
        snapshot => new Promise<CaptureValidationMessage[] | null>((resolve, reject) => this.requests.push({ snapshot, resolve, reject })),
        messages => this.published.push(messages));

    edit(declaration: string, eventStore = 'store') {
        this.validation.schedule({ eventStore, declaration });
    }

    pass(milliseconds = validationDelayMs) {
        this.clock.tick(milliseconds);
    }

    async settle() {
        await Promise.resolve();
        await Promise.resolve();
    }

    dispose() {
        this.clock.restore();
    }
}
