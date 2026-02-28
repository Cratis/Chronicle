// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import type { JsonSchema } from '../JsonSchema';
import type { ReadModelInfo } from './index';

export interface DraftReadModelInfo {
    identifier: string;
    displayName: string;
    containerName: string;
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
            identifier: schema.title || schema.name || '',
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

        // Check for diagnostics about undefined read models (errors)
        const notFoundDiagnostics = context.markers.filter(
            (marker: Monaco.editor.IMarkerData) => marker.message.includes("Read model") && marker.message.includes("not found")
        );

        // Check for diagnostics about draft read models (info markers)
        const draftDiagnostics = context.markers.filter(
            (marker: Monaco.editor.IMarkerData) => marker.message.includes("Read model") && marker.message.includes("is a draft")
        );

        // Handle "not found" errors - offer to create
        for (const diagnostic of notFoundDiagnostics) {
            const match = diagnostic.message.match(/Read model ['"]([\w.]+)['"]/);
            if (match && match[1]) {
                const readModelName = match[1];

                // Check if this read model already exists as a persisted read model
                const existsAsPersisted = this.readModels.some(
                    rm => rm.identifier === readModelName || rm.displayName === readModelName
                );

                if (!existsAsPersisted && this.onCreateReadModel) {
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

        // Handle draft read model info markers - offer to edit
        for (const diagnostic of draftDiagnostics) {
            const match = diagnostic.message.match(/Read model ['"]([\w.]+)['"]/);
            if (match && match[1]) {
                const readModelName = match[1];

                if (this.draftReadModel && this.isDraftReadModel(readModelName) && this.onEditReadModel) {
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
                }
            }
        }

        return {
            actions,
            dispose: () => { /* No cleanup needed */ }
        };
    }

    private isDraftReadModel(readModelToken: string): boolean {
        return !!this.draftReadModel && (this.draftReadModel.identifier === readModelToken || this.draftReadModel.displayName === readModelToken);
    }
}
