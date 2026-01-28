// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import type { JsonSchema } from '../JsonSchema';
import type { ReadModelInfo } from './index';

export class ProjectionDefinitionLanguageCodeActionProvider {
    private readModels: ReadModelInfo[] = [];
    private onCreateReadModel?: (readModelName: string) => void;

    setReadModels(readModels: ReadModelInfo[]): void {
        this.readModels = readModels;
    }

    // Keep for backwards compatibility
    setReadModelSchemas(schemas: JsonSchema[]): void {
        this.readModels = schemas.map(schema => ({
            displayName: schema.title || schema.name || '',
            schema
        }));
    }

    setCreateReadModelCallback(callback: (readModelName: string) => void): void {
        this.onCreateReadModel = callback;
    }

    invokeCreateReadModel(readModelName: string): void {
        this.onCreateReadModel?.(readModelName);
    }

    provideCodeActions(
        _model: Monaco.editor.ITextModel,
        _range: Monaco.Range,
        context: Monaco.languages.CodeActionContext
    ): Monaco.languages.ProviderResult<Monaco.languages.CodeActionList> {
        const actions: Monaco.languages.CodeAction[] = [];

        // Check if we have any diagnostics for undefined read models
        const diagnostics = context.markers.filter(
            (marker: Monaco.editor.IMarkerData) => marker.message.includes('not found in available schemas')
        );

        if (diagnostics.length > 0) {
            for (const diagnostic of diagnostics) {
                // Extract the read model name from the error message
                const match = diagnostic.message.match(/['"](\w+)['"]/);
                if (match && match[1]) {
                    const readModelName = match[1];

                    // Check if this read model already exists
                    const exists = this.readModels.some(
                        rm => rm.displayName === readModelName
                    );

                    if (!exists && this.onCreateReadModel) {
                        actions.push({
                            title: `Create read model '${readModelName}'`,
                            diagnostics: [diagnostic],
                            kind: 'quickfix',
                            edit: undefined, // No automatic edit - we'll show a dialog
                            isPreferred: true,
                            command: {
                                id: 'projection-declaration.createReadModel',
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
            dispose: () => { /* No cleanup needed */ }
        };
    }
}
