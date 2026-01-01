// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor, languages, IRange, Position } from 'monaco-editor';
import type { JsonSchema } from '../JsonSchema';

export interface PropertySchema {
    type: string;
    format?: string;
    items?: PropertySchema;
}

export class ProjectionDslValidator {
    private readModelSchemas: JsonSchema[] = [];

    setSchema(schema: JsonSchema): void {
        this.readModelSchemas = [schema];
    }

    setReadModelSchemas(schemas: JsonSchema[]): void {
        if (!schemas) {
            this.readModelSchemas = [];
            return;
        }
        let arr: JsonSchema[];
        if (!Array.isArray(schemas)) {
            const maybe = schemas as unknown as JsonSchema;
            arr = maybe ? [maybe] : [];
        } else {
            arr = schemas.filter((s) => s != null) as JsonSchema[];
        }
        this.readModelSchemas = arr.map((s) => ({ ...(s as any) } as JsonSchema));
    }

    setEventSchemas(_schemas: Record<string, JsonSchema> | JsonSchema[]): void {
        // Event schemas are not needed for validation in the new DSL
        // They're only used by the completion provider
    }

    validate(model: editor.ITextModel): editor.IMarkerData[] {
        const markers: editor.IMarkerData[] = [];
        const content = model.getValue();
        const lines = content.split('\n');

        // Basic validation: ensure it starts with 'projection'
        if (lines.length === 0 || !lines[0].trim()) {
            markers.push({
                severity: 8,
                startLineNumber: 1,
                startColumn: 1,
                endLineNumber: 1,
                endColumn: 1,
                message: 'Projection definition must start with "projection <ReadModelName>"',
            });
            return markers;
        }

        const firstLine = lines[0].trim();
        if (!firstLine.startsWith('projection ')) {
            markers.push({
                severity: 8,
                startLineNumber: 1,
                startColumn: 1,
                endLineNumber: 1,
                endColumn: firstLine.length + 1,
                message: 'Projection definition must start with "projection <ReadModelName>"',
            });
        }

        // Additional validation can be added here
        // For now, we keep it simple to avoid blocking the user

        return markers;
    }

    private getSchemaName(s: JsonSchema): string | undefined {
        if ((s as any).name) return (s as any).name;
        if ((s as any).title) return (s as any).title;
        if (typeof (s as any).$id === 'string') {
            const parts = (s as any).$id.split('/');
            return parts[parts.length - 1] || (s as any).$id;
        }
        return undefined;
    }
}

export class ProjectionDslCompletionProvider implements languages.CompletionItemProvider {
    private schema: JsonSchema | undefined;
    private readModelSchemas: JsonSchema[] = [];
    private eventSchemas: Record<string, JsonSchema> = {};

    setSchema(schema: JsonSchema): void {
        this.readModelSchemas = [schema];
        this.schema = schema;
    }

    setReadModelSchemas(schemas: JsonSchema[]): void {
        this.readModelSchemas = schemas || [];
    }

    setEventSchemas(schemas: Record<string, JsonSchema> | JsonSchema[]): void {
        if (!schemas) {
            this.eventSchemas = {};
            return;
        }

        if (Array.isArray(schemas)) {
            const map: Record<string, JsonSchema> = {};
            schemas.forEach((s, i) => {
                if (!s) return;
                const name = (s as any).title || (s as any).name || (typeof (s as any).$id === 'string' ? (s as any).$id.split('/').pop() : `Event${i + 1}`);
                map[name] = s;
            });
            this.eventSchemas = map;
            return;
        }

        this.eventSchemas = schemas || {};
    }

    provideCompletionItems(
        model: editor.ITextModel,
        position: Position
    ): languages.ProviderResult<languages.CompletionList> {
        const currentLine = model.getLineContent(position.lineNumber);
        const textBeforeCursor = currentLine.substring(0, position.column - 1);
        const suggestions: languages.CompletionItem[] = [];

        // Helper to get schema name
        const getSchemaName = (s?: JsonSchema, idx?: number) => {
            if (!s) return undefined;
            if ((s as any).name) return (s as any).name;
            if ((s as any).title) return (s as any).title;
            if (typeof (s as any).$id === 'string') {
                const parts = (s as any).$id.split('/');
                return parts[parts.length - 1] || (s as any).$id;
            }
            return idx !== undefined ? `ReadModel${idx + 1}` : undefined;
        };

        // Determine active schema based on projection declaration
        let activeSchema: JsonSchema | undefined = this.schema as any;
        const firstLine = model.getLineContent(1).trim();
        const projectionMatch = firstLine.match(/^projection\s+(\w+)/);
        if (projectionMatch && this.readModelSchemas.length > 0) {
            const readModelName = projectionMatch[1];
            const matched = this.readModelSchemas.find((s, i) => getSchemaName(s, i) === readModelName);
            if (matched) activeSchema = matched as any;
        }

        // Line 1: suggest "projection" keyword or read model names after "projection "
        if (position.lineNumber === 1) {
            if (textBeforeCursor.trim() === '' || textBeforeCursor.trim() === 'p' || textBeforeCursor.startsWith('proj')) {
                suggestions.push({
                    label: 'projection',
                    kind: 14, // Keyword
                    insertText: 'projection ',
                    documentation: 'Start a projection definition',
                    range: this.getWordRange(model, position),
                });
            }

            // After "projection ", suggest read model names
            if (textBeforeCursor.trim().startsWith('projection ')) {
                const word = model.getWordUntilPosition(position);
                this.readModelSchemas.forEach((s, i) => {
                    const display = getSchemaName(s, i);
                    if (!display) return;
                    if (!word.word || display.startsWith(word.word)) {
                        suggestions.push({
                            label: display,
                            kind: 6, // Class
                            insertText: display,
                            documentation: `Read model: ${display}`,
                            range: {
                                startLineNumber: position.lineNumber,
                                startColumn: word.startColumn,
                                endLineNumber: position.lineNumber,
                                endColumn: word.endColumn,
                            },
                        });
                    }
                });
            }

            return { suggestions };
        }

        // Suggest DSL keywords at the start of lines
        if (textBeforeCursor.trim() === '' || /^\s+$/.test(textBeforeCursor)) {
            // Suggest context-appropriate keywords
            suggestions.push(
                {
                    label: 'every',
                    kind: 14,
                    insertText: 'every ',
                    documentation: 'Define event handler for all events of a type',
                    range: this.getWordRange(model, position),
                },
                {
                    label: 'on',
                    kind: 14,
                    insertText: 'on ',
                    documentation: 'Define event handler for specific event',
                    range: this.getWordRange(model, position),
                },
                {
                    label: 'key',
                    kind: 14,
                    insertText: 'key ',
                    documentation: 'Define projection key property',
                    range: this.getWordRange(model, position),
                },
                {
                    label: 'automap',
                    kind: 14,
                    insertText: 'automap',
                    documentation: 'Automatically map event properties to read model',
                    range: this.getWordRange(model, position),
                },
                {
                    label: 'children',
                    kind: 14,
                    insertText: 'children ',
                    documentation: 'Define child collection operations',
                    range: this.getWordRange(model, position),
                },
                {
                    label: 'parent',
                    kind: 14,
                    insertText: 'parent ',
                    documentation: 'Define parent relationship',
                    range: this.getWordRange(model, position),
                }
            );

            // Suggest property names for assignments
            if (activeSchema && activeSchema.properties) {
                Object.keys(activeSchema.properties).forEach((propName) => {
                    const propSchema = activeSchema!.properties![propName];
                    suggestions.push({
                        label: propName,
                        kind: 9, // Property
                        insertText: `${propName} = `,
                        documentation: `Property: ${propName} (${propSchema.type}${propSchema.format ? `:${propSchema.format}` : ''})`,
                        range: this.getWordRange(model, position),
                    });
                });
            }

            return { suggestions };
        }

        // After "every " or "on ", suggest event type names
        if (textBeforeCursor.match(/\b(every|on)\s+\w*$/)) {
            const word = model.getWordUntilPosition(position);
            Object.keys(this.eventSchemas).forEach((eventName) => {
                if (!word.word || eventName.startsWith(word.word)) {
                    suggestions.push({
                        label: eventName,
                        kind: 6, // Class
                        insertText: eventName,
                        documentation: `Event type: ${eventName}`,
                        range: {
                            startLineNumber: position.lineNumber,
                            startColumn: word.startColumn,
                            endLineNumber: position.lineNumber,
                            endColumn: word.endColumn,
                        },
                    });
                }
            });
            return { suggestions };
        }

        // After "key ", suggest property names
        if (textBeforeCursor.match(/\bkey\s+\w*$/)) {
            if (activeSchema && activeSchema.properties) {
                const word = model.getWordUntilPosition(position);
                Object.keys(activeSchema.properties).forEach((propName) => {
                    if (!word.word || propName.startsWith(word.word)) {
                        const propSchema = activeSchema!.properties![propName];
                        suggestions.push({
                            label: propName,
                            kind: 9,
                            insertText: propName,
                            documentation: `Property: ${propName} (${propSchema.type})`,
                            range: {
                                startLineNumber: position.lineNumber,
                                startColumn: word.startColumn,
                                endLineNumber: position.lineNumber,
                                endColumn: word.endColumn,
                            },
                        });
                    }
                });
            }
            return { suggestions };
        }

        // After "children " or "parent ", suggest collection/parent property names
        if (textBeforeCursor.match(/\b(children|parent|join)\s+\w*$/)) {
            if (activeSchema && activeSchema.properties) {
                const word = model.getWordUntilPosition(position);
                Object.keys(activeSchema.properties).forEach((propName) => {
                    const propSchema = activeSchema!.properties![propName];
                    if (!word.word || propName.startsWith(word.word)) {
                        suggestions.push({
                            label: propName,
                            kind: 9,
                            insertText: propName,
                            documentation: `Property: ${propName} (${propSchema.type})`,
                            range: {
                                startLineNumber: position.lineNumber,
                                startColumn: word.startColumn,
                                endLineNumber: position.lineNumber,
                                endColumn: word.endColumn,
                            },
                        });
                    }
                });
            }
            return { suggestions };
        }

        // Assignment RHS: property = <cursor>
        if (textBeforeCursor.match(/\w+\s*=\s*\w*$/)) {
            const word = model.getWordUntilPosition(position);

            // Suggest event type names
            Object.keys(this.eventSchemas).forEach((eventName) => {
                if (!word.word || eventName.startsWith(word.word)) {
                    suggestions.push({
                        label: eventName,
                        kind: 6,
                        insertText: eventName + '.',
                        documentation: `Event type: ${eventName}`,
                        range: {
                            startLineNumber: position.lineNumber,
                            startColumn: word.startColumn,
                            endLineNumber: position.lineNumber,
                            endColumn: word.endColumn,
                        },
                    });
                }
            });

            // Suggest $eventSourceId, $causedBy, etc.
            [
                { name: '$eventSourceId', doc: 'The event source identifier' },
                { name: '$causedBy', doc: 'Who caused the event' },
                { name: '$occurred', doc: 'When the event occurred' },
                { name: '$namespace', doc: 'The event namespace' },
            ].forEach((builtin) => {
                if (!word.word || builtin.name.startsWith(word.word)) {
                    suggestions.push({
                        label: builtin.name,
                        kind: 14,
                        insertText: builtin.name,
                        documentation: builtin.doc,
                        range: {
                            startLineNumber: position.lineNumber,
                            startColumn: word.startColumn,
                            endLineNumber: position.lineNumber,
                            endColumn: word.endColumn,
                        },
                    });
                }
            });

            return { suggestions };
        }

        // After EventType., suggest event properties
        const eventPropMatch = textBeforeCursor.match(/(\w+)\.(\w*)$/);
        if (eventPropMatch) {
            const eventName = eventPropMatch[1];
            const typedProp = eventPropMatch[2] || '';
            const eventSchema = this.eventSchemas[eventName];
            if (eventSchema && eventSchema.properties) {
                Object.keys(eventSchema.properties).forEach((propName) => {
                    if (!typedProp || propName.startsWith(typedProp)) {
                        const prop = eventSchema.properties![propName];
                        suggestions.push({
                            label: propName,
                            kind: 9,
                            insertText: propName,
                            documentation: `Event property: ${propName} (${prop.type}${prop.format ? `:${prop.format}` : ''})`,
                            range: this.getWordRange(model, position),
                        });
                    }
                });
            }
            return { suggestions };
        }

        return { suggestions };
    }

    private getWordRange(model: editor.ITextModel, position: Position): IRange {
        const word = model.getWordUntilPosition(position);
        return {
            startLineNumber: position.lineNumber,
            startColumn: word.startColumn,
            endLineNumber: position.lineNumber,
            endColumn: word.endColumn,
        };
    }
}
