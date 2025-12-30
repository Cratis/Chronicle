// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import { configuration, languageId, monarchLanguage } from './language';
import { ProjectionDslCompletionProvider, ProjectionDslValidator, type ReadModelSchema } from './validation';
import type { JsonSchema } from '../JsonSchema';

let validator: ProjectionDslValidator;
let completionProvider: ProjectionDslCompletionProvider;
let disposables: Monaco.IDisposable[] = [];

export * from './ProjectionEditor';

export function registerProjectionDslLanguage(monaco: typeof Monaco): void {
    // Register the language
    monaco.languages.register({ id: languageId });

    // Set the language configuration
    monaco.languages.setLanguageConfiguration(languageId, configuration);

    // Set the Monarch tokenizer
    monaco.languages.setMonarchTokensProvider(languageId, monarchLanguage);

    // Create validator and completion provider
    validator = new ProjectionDslValidator();
    completionProvider = new ProjectionDslCompletionProvider();

    // Register completion provider with helpful trigger characters
    const completionDisposable = monaco.languages.registerCompletionItemProvider(languageId, {
        provideCompletionItems: completionProvider.provideCompletionItems.bind(completionProvider),
        triggerCharacters: ['.', ' ', '=', '['],
    });
    disposables.push(completionDisposable);

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

export function setReadModelSchema(schema: ReadModelSchema): void {
    // Backwards compatible single-schema setter
    if (validator) {
        validator.setReadModelSchemas([schema]);
    }
    if (completionProvider) {
        completionProvider.setReadModelSchemas([schema]);
    }
}

export function setReadModelSchemas(schemas: ReadModelSchema[]): void {
    try {
        // eslint-disable-next-line no-console
        console.log('[ProjectionDsl] setReadModelSchemas called', { type: typeof schemas, length: (schemas && (schemas as any).length) });
    } catch (e) {}
    if (validator) {
        validator.setReadModelSchemas(schemas);
    }
    if (completionProvider) {
        completionProvider.setReadModelSchemas(schemas);
    }
}

export function setEventSchemas(eventSchemas: Record<string, JsonSchema>): void {
    if (validator) {
        validator.setEventSchemas(eventSchemas);
    }
    if (completionProvider) {
        completionProvider.setEventSchemas(eventSchemas);
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
export type { ReadModelSchema, PropertySchema } from './validation';
