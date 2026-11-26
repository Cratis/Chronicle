// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CodeGenerationRequests, type GeneratedCode, type GenerationContext } from '../../codeGenerationRequests';

export type Deferred = { context: GenerationContext; resolve: (code: GeneratedCode) => void; reject: (error: unknown) => void };

export const code = (text: string): GeneratedCode => ({ declarative: `d:${text}`, modelBound: `m:${text}` });

export const context = (declaration: string, language: GenerationContext['language'] = 'CSharp'): GenerationContext =>
    ({ eventStore: 'store', namespace: 'ns', declaration, language });

export class a_harness {
    readonly clock = sinon.useFakeTimers();
    readonly requests: Deferred[] = [];
    readonly published: (GeneratedCode | null)[] = [];
    readonly generation = new CodeGenerationRequests(
        ctx => new Promise<GeneratedCode>((resolve, reject) => this.requests.push({ context: ctx, resolve, reject })),
        output => this.published.push(output));

    get visible(): GeneratedCode | null | undefined {
        return this.published[this.published.length - 1];
    }

    async settle() {
        await Promise.resolve();
        await Promise.resolve();
    }

    dispose() {
        this.clock.restore();
    }
}
