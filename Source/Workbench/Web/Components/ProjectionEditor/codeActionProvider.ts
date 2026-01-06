// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import type { JsonSchema } from '../JsonSchema';

export class ProjectionDslCodeActionProvider {
    private readModelSchemas: JsonSchema[] = [];
    private onCreateReadModel?: (readModelName: string) => void;

    setReadModelSchemas(schemas: JsonSchema[]): void {
        this.readModelSchemas = schemas;
    }

    setCreateReadModelCallback(callback: (readModelName: string) => void): void {
        this.onCreateReadModel = callback;
    }

    provideCodeActions(
        model: Monaco.editor.ITextModel,
        range: Monaco.Range,
        context: Monaco.languages.CodeActionContext,
        token: Monaco.CancellationToken
    ): Monaco.languages.ProviderResult<Monaco.languages.CodeActionList> {
        const actions: Monaco.languages.CodeAction[] = [];

        // Check if we have any diagnostics for undefined read models
        const diagnostics = context.markers.filter(
            marker => marker.message.includes('not found in available schemas')
        );

        if (diagnostics.length > 0) {
            for (const diagnostic of diagnostics) {
                // Extract the read model name from the error message
                const match = diagnostic.message.match(/['"](\w+)['"]/);
                if (match && match[1]) {
                    const readModelName = match[1];

                    // Check if this read model already exists
                    const exists = this.readModelSchemas.some(
                        schema => (schema as any).title === readModelName
                    );

                    if (!exists && this.onCreateReadModel) {
                        actions.push({
                            title: `Create read model '${readModelName}'`,
                            diagnostics: [diagnostic],
                            kind: 'quickfix',
                            edit: undefined, // No automatic edit - we'll show a dialog
                            isPreferred: true,
                            command: {
                                id: 'projection-dsl.createReadModel',
                                title: `Create ${readModelName}`,
                                arguments: [readModelName]
                            }
                        });
                    }
                }
            }
        }

        return {
            actions,
            dispose: () => { }
        };
    }
}
