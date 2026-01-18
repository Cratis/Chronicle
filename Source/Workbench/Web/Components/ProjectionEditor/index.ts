// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import { configuration, languageId, monarchLanguage } from './language';
import { ProjectionDslCompletionProvider } from './completionProvider';
import { ProjectionDslValidator } from './validator';
import { ProjectionDslHoverProvider } from './hoverProvider';
import { ProjectionDslCodeActionProvider } from './codeActionProvider';
import type { JsonSchema } from '../JsonSchema';

export interface ReadModelInfo {
    displayName: string;
    schema: JsonSchema;
}

let validator: ProjectionDslValidator;
let completionProvider: ProjectionDslCompletionProvider;
let hoverProvider: ProjectionDslHoverProvider;
let codeActionProvider: ProjectionDslCodeActionProvider;
let disposables: Monaco.IDisposable[] = [];

export * from './ProjectionEditor';

export function registerProjectionDslLanguage(monaco: typeof Monaco): void {
    // Register the language
    monaco.languages.register({ id: languageId });

    // Set the language configuration
    monaco.languages.setLanguageConfiguration(languageId, configuration);

    // Set the Monarch tokenizer
    monaco.languages.setMonarchTokensProvider(languageId, monarchLanguage);

    // Create validator, completion provider, and hover provider
    validator = new ProjectionDslValidator();
    completionProvider = new ProjectionDslCompletionProvider();
    hoverProvider = new ProjectionDslHoverProvider();
    codeActionProvider = new ProjectionDslCodeActionProvider();

    // Register completion provider with helpful trigger characters
    const completionDisposable = monaco.languages.registerCompletionItemProvider(languageId, {
        provideCompletionItems: completionProvider.provideCompletionItems.bind(completionProvider),
        triggerCharacters: ['.', ' ', '=', '\n', '$'],
    });
    disposables.push(completionDisposable);

    // Register hover provider
    const hoverDisposable = monaco.languages.registerHoverProvider(languageId, {
        provideHover: hoverProvider.provideHover.bind(hoverProvider),
    });
    disposables.push(hoverDisposable);

    // Register code action provider
    const codeActionDisposable = monaco.languages.registerCodeActionProvider(languageId, {
        provideCodeActions: codeActionProvider.provideCodeActions.bind(codeActionProvider),
    });
    disposables.push(codeActionDisposable);

    // Register command for creating read models
    const commandDisposable = monaco.editor.registerCommand('projection-dsl.createReadModel', (_accessor, readModelName: string) => {
        if (codeActionProvider) {
            (codeActionProvider as any).onCreateReadModel?.(readModelName);
        }
    });
    disposables.push(commandDisposable);

    // Register validation on model change
    const modelChangeDisposable = monaco.editor.onDidCreateModel((model) => {
        if (model.getLanguageId() === languageId) {
            validateModel(monaco, model);

            // Validate on content change
            const changeDisposable = model.onDidChangeContent(() => {
                validateModel(monaco, model);
            });
            disposables.push(changeDisposable);
        }
    });
    disposables.push(modelChangeDisposable);

    // Validate existing models
    monaco.editor.getModels().forEach((model) => {
        if (model.getLanguageId() === languageId) {
            validateModel(monaco, model);
        }
    });
}

export function setReadModelSchema(schema: JsonSchema): void {
    // Backwards compatible single-schema setter
    if (validator) {
        validator.setReadModelSchemas([schema]);
    }
    if (completionProvider) {
        completionProvider.setReadModelSchemas([schema]);
    }
}

export function setReadModelSchemas(readModels: ReadModelInfo[]): void {
    if (validator) {
        validator.setReadModels(readModels);
    }
    if (completionProvider) {
        completionProvider.setReadModels(readModels);
    }
    if (hoverProvider) {
        hoverProvider.setReadModels(readModels);
    }
    if (codeActionProvider) {
        codeActionProvider.setReadModels(readModels);
    }
}

export function setCreateReadModelCallback(callback: (readModelName: string) => void): void {
    if (codeActionProvider) {
        codeActionProvider.setCreateReadModelCallback(callback);
    }
}

export function setEventSchemas(eventSchemas: JsonSchema[] | Record<string, JsonSchema>): void {
    // Normalize either an array of schemas or a keyed record into a record keyed by derived schema name
    const normalize = (input: JsonSchema[] | Record<string, JsonSchema>) => {
        if (!input) return {} as Record<string, JsonSchema>;
        if (Array.isArray(input)) {
            const out: Record<string, JsonSchema> = {};
            input.forEach((s, i) => {
                if (!s) return;
                const name = (s as any).title || (s as any).name || (typeof (s as any).$id === 'string' ? (s as any).$id.split('/').pop() : `Event${i + 1}`);
                out[name] = s;
            });
            return out;
        }
        return input as Record<string, JsonSchema>;
    };

    const normalized = normalize(eventSchemas);
    if (validator) {
        validator.setEventSchemas(normalized);
    }
    if (completionProvider) {
        completionProvider.setEventSchemas(normalized);
    }
    if (hoverProvider) {
        hoverProvider.setEventSchemas(normalized);
    }
}

export function setEventSequences(sequences: string[]): void {
    if (completionProvider) {
        completionProvider.setEventSequences(sequences);
    }
}

export function disposeProjectionDslLanguage(): void {
    disposables.forEach((d) => d.dispose());
    disposables = [];
}

function validateModel(monaco: typeof Monaco, model: Monaco.editor.ITextModel): void {
    if (validator) {
        const markers = validator.validate(model);
        monaco.editor.setModelMarkers(model, languageId, markers);
    }
}

export { languageId };
export type { JsonSchema } from '../JsonSchema';
