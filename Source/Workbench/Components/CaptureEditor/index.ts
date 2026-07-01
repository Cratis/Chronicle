// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import { configuration, languageId, monarchLanguage } from './language';
import { CaptureDefinitionLanguageCompletionProvider } from './CaptureDefinitionLanguageCompletionProvider';
import { CaptureDefinitionLanguageValidator } from './CaptureDefinitionLanguageValidator';
import { CaptureDefinitionLanguageHoverProvider } from './CaptureDefinitionLanguageHoverProvider';

let validator: CaptureDefinitionLanguageValidator;
let completionProvider: CaptureDefinitionLanguageCompletionProvider;
let hoverProvider: CaptureDefinitionLanguageHoverProvider;
let disposables: Monaco.IDisposable[] = [];

export * from './CaptureEditor';

let isRegistered = false;

export function registerCaptureDefinitionLanguage(monaco: typeof Monaco): void {
    if (isRegistered) {
        return;
    }

    isRegistered = true;

    monaco.languages.register({ id: languageId });
    monaco.languages.setLanguageConfiguration(languageId, configuration);
    monaco.languages.setMonarchTokensProvider(languageId, monarchLanguage);

    validator = new CaptureDefinitionLanguageValidator();
    completionProvider = new CaptureDefinitionLanguageCompletionProvider();
    hoverProvider = new CaptureDefinitionLanguageHoverProvider();

    const completionDisposable = monaco.languages.registerCompletionItemProvider(languageId, {
        provideCompletionItems: completionProvider.provideCompletionItems.bind(completionProvider),
        triggerCharacters: ['.', ' ', '=', '$'],
    });
    disposables.push(completionDisposable);

    const hoverDisposable = monaco.languages.registerHoverProvider(languageId, {
        provideHover: hoverProvider.provideHover.bind(hoverProvider),
    });
    disposables.push(hoverDisposable);

    const modelChangeDisposable = monaco.editor.onDidCreateModel((model: Monaco.editor.ITextModel) => {
        if (model.getLanguageId() === languageId) {
            validateModel(monaco, model);
            const changeDisposable = model.onDidChangeContent(() => {
                validateModel(monaco, model);
            });
            disposables.push(changeDisposable);
        }
    });
    disposables.push(modelChangeDisposable);

    monaco.editor.getModels().forEach((model: Monaco.editor.ITextModel) => {
        if (model.getLanguageId() === languageId) {
            validateModel(monaco, model);
        }
    });
}

export function disposeCaptureDefinitionLanguage(): void {
    disposables.forEach(disposable => disposable.dispose());
    disposables = [];
    isRegistered = false;
}

function validateModel(monaco: typeof Monaco, model: Monaco.editor.ITextModel): void {
    if (validator && model && !model.isDisposed()) {
        const markers = validator.validate(model);
        monaco.editor.setModelMarkers(model, 'capture-declaration-validator', markers);
    }
}

export { languageId };
