// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor, languages, IRange, Position } from 'monaco-editor';
import type { JsonSchema } from '../JsonSchema';

export interface PropertySchema {
    type: string;
    format?: string;
    items?: PropertySchema;
}

export interface Token {
    type: string;
    value: string;
    line: number;
    column: number;
}

const keywords = [
    'key',
    'increment',
    'decrement',
    'count',
    'by',
    'on',
    'join',
    'identified',
    'removedWith',
];

const numericFormats = [
    'int16',
    'int32',
    'uint32',
    'int64',
    'uint64',
    'float',
    'double',
    'decimal',
    'byte',
    'duration', // TimeSpan
];

export class ProjectionDslValidator {
    private schema: JsonSchema = {};
    private readModelSchemas: JsonSchema[] = [];
    private eventSchemas: JsonSchema[] = [];

    setSchema(schema: JsonSchema): void {
        // Backwards compatible single-schema setter
        this.readModelSchemas = [schema];
        this.schema = schema;
    }

    setReadModelSchemas(schemas: JsonSchema[]): void {
        // Normalize input: ensure array of objects and filter out undefined slots
        if (!schemas) {
            this.readModelSchemas = [];
            return;
        }
        if (!Array.isArray(schemas)) {
            // if a single schema object was passed, wrap it
            const maybe = schemas as unknown as JsonSchema;
            this.readModelSchemas = maybe ? [maybe] : [];
            return;
        }
        this.readModelSchemas = schemas.filter((s) => s != null) as JsonSchema[];
    }

    setEventSchemas(schemas: JsonSchema[]): void {
        this.eventSchemas = schemas || [];
    }

    validate(model: editor.ITextModel): languages.IMarkerData[] {
        const markers: languages.IMarkerData[] = [];
        const content = model.getValue();
        const lines = content.split('\n');

        if (lines.length === 0 || !lines[0].trim()) {
            markers.push({
                severity: 8, // Error
                startLineNumber: 1,
                startColumn: 1,
                endLineNumber: 1,
                endColumn: 1,
                message: 'Missing read model name',
            });
            return markers;
        }

        // Validate read model name (first line)
        const readModelLine = lines[0].trim();

        // Select schema matching the declared read model name if available
        if (this.readModelSchemas && this.readModelSchemas.length > 0) {
            const matched = this.readModelSchemas.find((s) => s.name === readModelLine);
            if (matched) this.schema = matched;
            else if (this.readModelSchemas.length === 1 && !readModelLine) this.schema = this.readModelSchemas[0];
        }
        if (readModelLine.includes('|') || readModelLine.includes('=')) {
            markers.push({
                severity: 8,
                startLineNumber: 1,
                startColumn: 1,
                endLineNumber: 1,
                endColumn: readModelLine.length + 1,
                message: 'Read model name must be a simple identifier',
            });
        }

        // Validate each statement
        for (let i = 1; i < lines.length; i++) {
            const line = lines[i].trim();
            if (!line || line.startsWith('#')) continue;

            if (!line.startsWith('|')) {
                markers.push({
                    severity: 8,
                    startLineNumber: i + 1,
                    startColumn: 1,
                    endLineNumber: i + 1,
                    endColumn: Math.max(1, line.length + 1),
                    message: 'Statements must start with |',
                });
                continue;
            }

            const statement = line.substring(1).trim();
            this.validateStatement(statement, i + 1, markers);
        }

        return markers;
    }

    private validateStatement(
        statement: string,
        lineNumber: number,
        markers: languages.IMarkerData[]
    ): void {
        if (!statement) return;

        // Check for arithmetic operations on non-numeric properties
        if (this.schema) {
            const addMatch = statement.match(/^(\w+)\+(\w+)\.(\w+)$/);
            const subtractMatch = statement.match(/^(\w+)-(\w+)\.(\w+)$/);

            if (addMatch || subtractMatch) {
                const match = (addMatch || subtractMatch)!;
                const targetProperty = match[1];
                const sourceProperty = match[3];

                const targetSchema = this.schema?.properties[targetProperty];
                if (targetSchema && !this.isNumericType(targetSchema)) {
                    markers.push({
                        severity: 8,
                        startLineNumber: lineNumber,
                        startColumn: 1,
                        endLineNumber: lineNumber,
                        endColumn: statement.length + 1,
                        message: `Arithmetic operations can only be used on numeric or TimeSpan properties. Property '${targetProperty}' is of type '${targetSchema.type}'`,
                    });
                }
            }

            // Check increment/decrement
            const incrementMatch = statement.match(/^(\w+)\s+increment\s+by\s+(\w+)$/);
            const decrementMatch = statement.match(/^(\w+)\s+decrement\s+by\s+(\w+)$/);

            if (incrementMatch || decrementMatch) {
                const match = (incrementMatch || decrementMatch)!;
                const targetProperty = match[1];

                const targetSchema = this.schema?.properties[targetProperty];
                if (targetSchema && !this.isNumericType(targetSchema)) {
                    markers.push({
                        severity: 8,
                        startLineNumber: lineNumber,
                        startColumn: 1,
                        endLineNumber: lineNumber,
                        endColumn: statement.length + 1,
                        message: `Increment/decrement can only be used on numeric or TimeSpan properties. Property '${targetProperty}' is of type '${targetSchema.type}'`,
                    });
                }
            }

            // Check property assignments for type compatibility
            const assignMatch = statement.match(/^(\w+)=(\w+)\.(\w+)$/);
            if (assignMatch) {
                const targetProperty = assignMatch[1];
                const sourceProperty = assignMatch[3];

                const targetSchema = this.schema?.properties[targetProperty];
                // We can't validate source property type without event schema,
                // but we can suggest valid target properties
            }
        }
    }

    private isNumericType(schema: PropertySchema): boolean {
        if (schema.type === 'number' || schema.type === 'integer') {
            return true;
        }
        if (schema.format && numericFormats.includes(schema.format)) {
            return true;
        }
        return false;
    }
}

export class ProjectionDslCompletionProvider implements languages.CompletionItemProvider {
    private schema: ReadModelSchema | undefined;
    private readModelSchemas: ReadModelSchema[] = [];
    private eventSchemas: Record<string, JsonSchema> = {};

    setSchema(schema: ReadModelSchema): void {
        // Backwards compat
        this.readModelSchemas = [schema];
        this.schema = schema;
    }

    setReadModelSchemas(schemas: ReadModelSchema[]): void {
        this.readModelSchemas = schemas || [];
    }

    setEventSchemas(schemas: Record<string, JsonSchema>): void {
        this.eventSchemas = schemas || {};
    }

    provideCompletionItems(
        model: editor.ITextModel,
        position: Position
    ): languages.ProviderResult<languages.CompletionList> {
        console.log('[ProjectionDsl] provideCompletionItems called', { line: position.lineNumber, col: position.column });
        const textUntilPosition = model.getValueInRange({
            startLineNumber: 1,
            startColumn: 1,
            endLineNumber: position.lineNumber,
            endColumn: position.column,
        });

        const lines = textUntilPosition.split('\n');
        const currentLine = lines[lines.length - 1];

        const suggestions: languages.CompletionItem[] = [];

        // Selected read model is declared on the first line
        const declaredReadModel = model.getLineContent(1).trim();
        let activeSchema: ReadModelSchema | undefined = this.schema;
        if (this.readModelSchemas && this.readModelSchemas.length > 0) {
            const matched = this.readModelSchemas.find((s) => s.name === declaredReadModel);
            if (matched) activeSchema = matched;
            else if (!declaredReadModel && this.readModelSchemas.length === 1) activeSchema = this.readModelSchemas[0];
        }

        // If we are editing the first line, suggest available read model names
        console.log('[ProjectionDsl] readModelSchemas', (this.readModelSchemas || []).map(s => s.name));
        console.log('[ProjectionDsl] eventSchemas', Object.keys(this.eventSchemas || {}));
        if (position.lineNumber === 1 && this.readModelSchemas && this.readModelSchemas.length > 0) {
            const word = model.getWordUntilPosition(position);
            console.log('[ProjectionDsl] first-line declaredReadModel, word', declaredReadModel, word);
            this.readModelSchemas.forEach((s) => {
                if (!s.name) return;
                if (!word.word || s.name.startsWith(word.word)) {
                    suggestions.push({
                        label: s.name,
                        kind: 7, // Class/Type
                        insertText: s.name,
                        documentation: `Read model: ${s.name}`,
                        range: {
                            startLineNumber: position.lineNumber,
                            startColumn: word.startColumn,
                            endLineNumber: position.lineNumber,
                            endColumn: word.endColumn,
                        },
                    });
                }
            });

            console.log('[ProjectionDsl] first-line suggestions count', suggestions.length);
            return { suggestions };
        }

        try {
            console.debug('[ProjectionDsl] activeSchema', activeSchema && activeSchema.name);
        } catch (e) {}

        // If at the start of a new line after first line, suggest |
        if (position.lineNumber > 1 && currentLine.trim() === '') {
            suggestions.push({
                label: '|',
                kind: 27, // Operator
                insertText: '| ',
                documentation: 'Start a new statement',
                range: this.getWordRange(model, position),
            });
        }

        // After |, suggest keywords and property names
        if (currentLine.trim().startsWith('|')) {
            const afterPipe = currentLine.substring(currentLine.indexOf('|') + 1).trim();

            // Suggest keywords
            keywords.forEach((keyword) => {
                suggestions.push({
                    label: keyword,
                    kind: 14, // Keyword
                    insertText: keyword,
                    documentation: `Keyword: ${keyword}`,
                    range: this.getWordRange(model, position),
                });
            });

            // Suggest read model properties from the active schema
            if (activeSchema) {
                Object.keys(activeSchema.properties).forEach((propName) => {
                    const propSchema = activeSchema!.properties[propName];
                    suggestions.push({
                        label: propName,
                        kind: 10, // Property
                        insertText: propName,
                        documentation: `Property: ${propName} (${propSchema.type}${
                            propSchema.format ? `:${propSchema.format}` : ''
                        })`,
                        range: this.getWordRange(model, position),
                    });
                });
            }

            // Suggest operators based on context
            if (afterPipe && !afterPipe.includes('=') && !afterPipe.includes('+') && !afterPipe.includes('-')) {
                if (this.schema) {
                    const word = afterPipe.split(/\s+/)[0];
                    const propSchema = this.schema.properties[word];

                    if (propSchema) {
                        // Suggest = for all properties
                        suggestions.push({
                            label: '=',
                            kind: 27, // Operator
                            insertText: '=',
                            documentation: 'Set property value',
                            range: this.getWordRange(model, position),
                        });

                        // Suggest +/- only for numeric properties
                        if (this.isNumericType(propSchema)) {
                            suggestions.push({
                                label: '+',
                                kind: 27,
                                insertText: '+',
                                documentation: 'Add to property value',
                                range: this.getWordRange(model, position),
                            });

                            suggestions.push({
                                label: '-',
                                kind: 27,
                                insertText: '-',
                                documentation: 'Subtract from property value',
                                range: this.getWordRange(model, position),
                            });
                        }
                    }
                }
            }
        }

        // Suggest event type names when typing right-hand side before a dot
        const rhsMatch = currentLine.match(/=\s*(\w*)$/);
        if (rhsMatch && rhsMatch[1] !== undefined) {
            const typed = rhsMatch[1];
            Object.keys(this.eventSchemas).forEach((eventName) => {
                if (!typed || eventName.startsWith(typed)) {
                    suggestions.push({
                        label: eventName,
                        kind: 7, // Class/Type
                        insertText: eventName,
                        documentation: `Event type: ${eventName}`,
                        range: this.getWordRange(model, position),
                    });
                }
            });
        }

        // Suggest $eventContext
        if (currentLine.includes('=') && !currentLine.includes('$eventContext')) {
            suggestions.push({
                label: '$eventContext',
                kind: 14, // Keyword
                insertText: '$eventContext.',
                documentation: 'Access event context properties',
                range: this.getWordRange(model, position),
            });
        }

        // After $eventContext., suggest context properties
        if (currentLine.includes('$eventContext.')) {
            const contextProperties = [
                { name: 'occurred', doc: 'When the event occurred' },
                { name: 'eventSourceId', doc: 'The event source identifier' },
                { name: 'causedBy', doc: 'Who caused the event' },
                { name: 'namespace', doc: 'The event namespace' },
            ];

            contextProperties.forEach((prop) => {
                suggestions.push({
                    label: prop.name,
                    kind: 10, // Property
                    insertText: prop.name,
                    documentation: prop.doc,
                    range: this.getWordRange(model, position),
                });
            });
        }

        // Suggest event properties when typing EventType.<prop>
        const eventPropMatch = currentLine.match(/(\w+)\.(\w*)$/);
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
                            kind: 10,
                            insertText: propName,
                            documentation: `Event property: ${propName} (${prop.type}${prop.format ? `:${prop.format}` : ''})`,
                            range: this.getWordRange(model, position),
                        });
                    }
                });
            }
        }

        return {
            suggestions,
        };
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

    private isNumericType(schema: PropertySchema): boolean {
        if (schema.type === 'number' || schema.type === 'integer') {
            return true;
        }
        if (schema.format && numericFormats.includes(schema.format)) {
            return true;
        }
        return false;
    }
}
