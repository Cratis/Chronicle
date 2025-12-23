// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import { configuration, languageId, monarchLanguage } from './language';
import { ProjectionDslCompletionProvider, ProjectionDslValidator, type ReadModelSchema } from './validation';

let validator: ProjectionDslValidator;
let completionProvider: ProjectionDslCompletionProvider;
let disposables: Monaco.IDisposable[] = [];

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

    // Register completion provider
    const completionDisposable = monaco.languages.registerCompletionItemProvider(languageId, completionProvider);
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
    if (validator) {
        validator.setSchema(schema);
    }
    if (completionProvider) {
        completionProvider.setSchema(schema);
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
