// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DraftReadModel } from 'Features/ProjectionEditor';
import type { ProjectionCodeLanguage } from './ProjectionCodeLanguages';

/** How long the declaration has to rest before code is generated for it. */
export const generationDelayMs = 300;

/** The text shown for an output that could not be generated. */
export const generationFailedCode = '// Unable to generate code';

/** Everything a generation request is made for. */
export interface GenerationContext {
    readonly eventStore: string;
    readonly namespace: string;
    readonly declaration: string;
    readonly language: ProjectionCodeLanguage;
    readonly draftReadModel?: DraftReadModel | null;
}

/** The generated outputs; they are always published as a pair. */
export interface GeneratedCode {
    readonly declarative: string;
    readonly modelBound: string;
}

/** The part of a generation command the requests depend on. */
export interface GenerationCommand {
    eventStore: string;
    namespace: string;
    declaration: string;
    language: string;
    draftReadModel?: DraftReadModel;
    execute(): Promise<{ response?: { code?: string } | null }>;
}

const generateWith = async (command: GenerationCommand, context: GenerationContext): Promise<string> => {
    command.eventStore = context.eventStore;
    command.namespace = context.namespace;
    command.declaration = context.declaration;
    command.language = context.language;
    command.draftReadModel = context.draftReadModel ?? undefined;
    const result = await command.execute();
    return result.response?.code || generationFailedCode;
};

/**
 * Generates both outputs for a context. Each command is set and dispatched in one synchronous step, so
 * a later context can never leak into this request through the shared commands.
 * @param declarative The declarative code command.
 * @param modelBound The model bound code command.
 * @param context The context to generate for.
 * @returns The generated pair.
 */
export const generateCode = async (declarative: GenerationCommand, modelBound: GenerationCommand, context: GenerationContext): Promise<GeneratedCode> => {
    const [declarativeCode, modelBoundCode] = await Promise.all([generateWith(declarative, context), generateWith(modelBound, context)]);
    return { declarative: declarativeCode, modelBound: modelBoundCode };
};

const keyOf = (context: GenerationContext): string => JSON.stringify([
    context.eventStore, context.namespace, context.declaration, context.language, context.draftReadModel ?? null,
]);

/**
 * Coalesces generation requests and only lets the outcome of the latest context reach the screen.
 * Edits are debounced; opening, a language change and a refresh run immediately.
 */
export class CodeGenerationRequests {
    #revision = 0;
    #timer: ReturnType<typeof setTimeout> | undefined;
    #key: string | undefined;

    constructor(
        private readonly _execute: (context: GenerationContext) => Promise<GeneratedCode>,
        private readonly _publish: (code: GeneratedCode | null) => void,
        private readonly _delayMs: number = generationDelayMs) {
    }

    /**
     * Generates for the context once the delay has passed. A context that is the one already requested
     * (compared by value, so a new but equivalent object changes nothing) is left alone.
     * @param context The context to generate for.
     */
    schedule(context: GenerationContext): void {
        const key = keyOf(context);
        if (key === this.#key) return;

        this.cancel();
        this.#key = key;
        const revision = this.#revision;
        this.#timer = setTimeout(() => {
            this.#timer = undefined;
            void this.#dispatch(context, revision);
        }, this._delayMs);
    }

    /**
     * Generates for the context now, replacing anything queued or running, and hides the output of the previous context.
     * @param context The context to generate for.
     */
    run(context: GenerationContext): void {
        this.cancel();
        this.#key = keyOf(context);
        this._publish(null);
        void this.#dispatch(context, this.#revision);
    }

    /** Supersedes everything requested so far, and hides the output. */
    clear(): void {
        this.cancel();
        this._publish(null);
    }

    /** Supersedes everything requested so far: queued work never runs and running work is ignored. */
    cancel(): void {
        this.#revision++;
        this.#key = undefined;
        if (this.#timer !== undefined) {
            clearTimeout(this.#timer);
            this.#timer = undefined;
        }
    }

    async #dispatch(context: GenerationContext, revision: number): Promise<void> {
        let code: GeneratedCode;
        try {
            code = await this._execute(context);
        } catch {
            code = { declarative: generationFailedCode, modelBound: generationFailedCode };
        }

        if (revision === this.#revision) {
            this._publish(code);
        }
    }
}
