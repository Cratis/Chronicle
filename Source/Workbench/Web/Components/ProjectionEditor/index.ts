// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import { configuration, languageId, monarchLanguage } from './language';
import { ProjectionDefinitionLanguageCompletionProvider } from './ProjectionDefinitionLanguageCompletionProvider';
import { ProjectionDefinitionLanguageValidator } from './ProjectionDefinitionLanguageValidator';
import { ProjectionDefinitionLanguageHoverProvider } from './ProjectionDefinitionLanguageHoverProvider';
import { ProjectionDefinitionLanguageCodeActionProvider } from './ProjectionDefinitionLanguageCodeActionProvider';
import type { JsonSchema } from '../JsonSchema';

export interface ReadModelInfo {
    displayName: string;
    schema: JsonSchema;
}

let validator: ProjectionDefinitionLanguageValidator;
let completionProvider: ProjectionDefinitionLanguageCompletionProvider;
let hoverProvider: ProjectionDefinitionLanguageHoverProvider;
let codeActionProvider: ProjectionDefinitionLanguageCodeActionProvider;
let disposables: Monaco.IDisposable[] = [];
let monacoInstance: typeof Monaco | null = null;
let pendingCreateReadModelCallback: ((readModelName: string) => void) | null = null;
let pendingEditReadModelCallback: ((readModelName: string, currentSchema: JsonSchema) => void) | null = null;

export * from './ProjectionEditor';

let isRegistered = false;

export function registerProjectionDefinitionLanguage(monaco: typeof Monaco): void {
    // Prevent duplicate registration
    if (isRegistered) {
        return;
    }
    isRegistered = true;
    monacoInstance = monaco;

    // Register the language
    monaco.languages.register({ id: languageId });

    // Set the language configuration
    monaco.languages.setLanguageConfiguration(languageId, configuration);

    // Set the Monarch tokenizer
    monaco.languages.setMonarchTokensProvider(languageId, monarchLanguage);

    // Create validator, completion provider, and hover provider
    validator = new ProjectionDefinitionLanguageValidator();
    completionProvider = new ProjectionDefinitionLanguageCompletionProvider();
    hoverProvider = new ProjectionDefinitionLanguageHoverProvider();
    codeActionProvider = new ProjectionDefinitionLanguageCodeActionProvider();

    // Apply pending callbacks if they were set before initialization
    if (pendingCreateReadModelCallback) {
        codeActionProvider.setCreateReadModelCallback(pendingCreateReadModelCallback);
        pendingCreateReadModelCallback = null;
    }
    if (pendingEditReadModelCallback) {
        codeActionProvider.setEditReadModelCallback(pendingEditReadModelCallback);
        pendingEditReadModelCallback = null;
    }

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
    }, {
        providedCodeActionKinds: ['quickfix']
    });
    disposables.push(codeActionDisposable);

    // Register command for creating read models
    const commandDisposable = monaco.editor.registerCommand('projection-declaration.createReadModel', (_accessor: unknown, readModelName: string) => {
        if (codeActionProvider) {
            codeActionProvider.invokeCreateReadModel(readModelName);
        }
    });
    disposables.push(commandDisposable);

    // Register command for editing read models
    const editCommandDisposable = monaco.editor.registerCommand('projection-declaration.editReadModel', (_accessor: unknown, readModelName: string, currentSchema: JsonSchema) => {
        if (codeActionProvider) {
            codeActionProvider.invokeEditReadModel(readModelName, currentSchema);
        }
    });
    disposables.push(editCommandDisposable);

    // Register validation on model change
    const modelChangeDisposable = monaco.editor.onDidCreateModel((model: Monaco.editor.ITextModel) => {
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
    monaco.editor.getModels().forEach((model: Monaco.editor.ITextModel) => {
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
    revalidateAllModels();
}

export function setCreateReadModelCallback(callback: (readModelName: string) => void): void {
    if (codeActionProvider) {
        codeActionProvider.setCreateReadModelCallback(callback);
    } else {
        // Store callback for later when the provider is initialized
        pendingCreateReadModelCallback = callback;
    }
}

export function setEditReadModelCallback(callback: (readModelName: string, currentSchema: JsonSchema) => void): void {
    if (codeActionProvider) {
        codeActionProvider.setEditReadModelCallback(callback);
    } else {
        // Store callback for later when the provider is initialized
        pendingEditReadModelCallback = callback;
    }
}

export function setDraftReadModel(draft: { containerName: string; schema: JsonSchema } | null): void {
    if (codeActionProvider) {
        codeActionProvider.setDraftReadModel(draft);
    }
    if (validator) {
        validator.setDraftReadModel(draft);
    }
    if (hoverProvider) {
        hoverProvider.setDraftReadModel(draft);
    }
    revalidateAllModels();
}

export function setEventSchemas(eventSchemas: JsonSchema[] | Record<string, JsonSchema>): void {
    // Normalize either an array of schemas or a keyed record into a record keyed by derived schema name
    const normalize = (input: JsonSchema[] | Record<string, JsonSchema>) => {
        if (!input) return {} as Record<string, JsonSchema>;
        if (Array.isArray(input)) {
            const out: Record<string, JsonSchema> = {};
            input.forEach((s, i) => {
                if (!s) return;
                const name = s.title || s.name || (typeof s.$id === 'string' ? s.$id.split('/').pop() : undefined) || `Event${i + 1}`;
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
    revalidateAllModels();
}

export function setEventSequences(sequences: string[]): void {
    if (completionProvider) {
        completionProvider.setEventSequences(sequences);
    }
}

export function disposeProjectionDefinitionLanguage(): void {
    disposables.forEach((d) => d.dispose());
    disposables = [];
    isRegistered = false;
    monacoInstance = null;
}

function validateModel(monaco: typeof Monaco, model: Monaco.editor.ITextModel): void {
    if (validator && model && !model.isDisposed()) {
        const markers = validator.validate(model);
        monaco.editor.setModelMarkers(model, 'projection-declaration-validator', markers);
    }
}

function revalidateAllModels(): void {
    if (monacoInstance) {
        monacoInstance.editor.getModels().forEach((model: Monaco.editor.ITextModel) => {
            if (model.getLanguageId() === languageId) {
                validateModel(monacoInstance!, model);
            }
        });
    }
}

export { languageId };
export type { JsonSchema } from '../JsonSchema';
