// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import type { JsonSchema } from '../JsonSchema';
import type { ReadModelInfo } from './index';

export interface DraftReadModelInfo {
    name: string;
    schema: JsonSchema;
}

export class ProjectionDefinitionLanguageCodeActionProvider {
    private readModels: ReadModelInfo[] = [];
    private draftReadModel: DraftReadModelInfo | null = null;
    private onCreateReadModel?: (readModelName: string) => void;
    private onEditReadModel?: (readModelName: string, currentSchema: JsonSchema) => void;

    setReadModels(readModels: ReadModelInfo[]): void {
        this.readModels = readModels;
    }

    setDraftReadModel(draft: DraftReadModelInfo | null): void {
        this.draftReadModel = draft;
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

    setEditReadModelCallback(callback: (readModelName: string, currentSchema: JsonSchema) => void): void {
        this.onEditReadModel = callback;
    }

    invokeCreateReadModel(readModelName: string): void {
        this.onCreateReadModel?.(readModelName);
    }

    invokeEditReadModel(readModelName: string, currentSchema: JsonSchema): void {
        this.onEditReadModel?.(readModelName, currentSchema);
    }

    provideCodeActions(
        _model: Monaco.editor.ITextModel,
        _range: Monaco.Range,
        context: Monaco.languages.CodeActionContext
    ): Monaco.languages.ProviderResult<Monaco.languages.CodeActionList> {
        const actions: Monaco.languages.CodeAction[] = [];

        // Check if we have any diagnostics for undefined read models
        const diagnostics = context.markers.filter(
            (marker: Monaco.editor.IMarkerData) => marker.message.includes("Read model") && marker.message.includes("not found")
        );

        if (diagnostics.length > 0) {
            for (const diagnostic of diagnostics) {
                // Extract the read model name from the error message
                const match = diagnostic.message.match(/Read model ['"](\w+)['"]/);
                if (match && match[1]) {
                    const readModelName = match[1];

                    // Check if this is a draft read model (can be edited)
                    const isDraft = this.draftReadModel && this.draftReadModel.name === readModelName;

                    // Check if this read model already exists as a persisted read model
                    const existsAsPersisted = this.readModels.some(
                        rm => rm.displayName === readModelName
                    );

                    if (isDraft && this.onEditReadModel) {
                        // Offer to edit the draft read model
                        actions.push({
                            title: `Edit read model '${readModelName}'`,
                            diagnostics: [diagnostic],
                            kind: 'quickfix',
                            edit: undefined,
                            isPreferred: true,
                            command: {
                                id: 'projection-declaration.editReadModel',
                                title: `Edit ${readModelName}`,
                                arguments: [readModelName, this.draftReadModel.schema]
                            }
                        });
                    } else if (!existsAsPersisted && !isDraft && this.onCreateReadModel) {
                        // Offer to create a new read model
                        actions.push({
                            title: `Create read model '${readModelName}'`,
                            diagnostics: [diagnostic],
                            kind: 'quickfix',
                            edit: undefined,
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
