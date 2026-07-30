// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import type { RefObject } from 'react';
import { languageId } from '@cratis/screenplay-language/capture';

/**
 * The dynamic completion values the provider reads at completion time.
 */
export interface DynamicCompletions {
    externalServiceNames: string[];
    eventTypeNames: string[];
}

const apiLinePattern = /^\s*api\s+[\w.]*$/;
const appendLinePattern = /^\s*append\s+[\w.]*$/;

/**
 * Registers a completion provider that suggests values known by the backend - external service
 * names after the `api` keyword and event type names after the `append` keyword. The values are
 * read through a ref so they can be refreshed without re-registering the provider.
 * @param monaco The Monaco instance to register with.
 * @param completions Ref holding the current completion values.
 * @returns A disposable that unregisters the provider.
 */
export function registerDynamicCompletions(
    monaco: typeof Monaco,
    completions: RefObject<DynamicCompletions>): { dispose(): void } {
    return monaco.languages.registerCompletionItemProvider(languageId, {
        triggerCharacters: [' '],
        provideCompletionItems(model, position) {
            const lineBeforeCursor = model.getLineContent(position.lineNumber).substring(0, position.column - 1);
            const current = completions.current;
            let values: string[] = [];

            if (apiLinePattern.test(lineBeforeCursor)) {
                values = current?.externalServiceNames ?? [];
            } else if (appendLinePattern.test(lineBeforeCursor)) {
                values = current?.eventTypeNames ?? [];
            }

            if (values.length === 0) {
                return { suggestions: [] };
            }

            const word = model.getWordUntilPosition(position);
            const range = {
                startLineNumber: position.lineNumber,
                endLineNumber: position.lineNumber,
                startColumn: word.startColumn,
                endColumn: word.endColumn,
            };

            return {
                suggestions: values.map(value => ({
                    label: value,
                    kind: monaco.languages.CompletionItemKind.Value,
                    insertText: value,
                    range,
                })),
            };
        },
    });
}
