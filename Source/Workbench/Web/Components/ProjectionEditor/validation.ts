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
    'identifier',
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
    private eventSchemas: Record<string, JsonSchema> = {};

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
        let arr: JsonSchema[];
        if (!Array.isArray(schemas)) {
            const maybe = schemas as unknown as JsonSchema;
            arr = maybe ? [maybe] : [];
        } else {
            arr = schemas.filter((s) => s != null) as JsonSchema[];
        }

        // Ensure each schema has a name for suggestions: prefer explicit name, then title, then $id last segment, else fallback to indexed name
        this.readModelSchemas = arr.map((s, i) => ({ ...(s as any) } as JsonSchema));
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

        // Helper to derive a display name for a schema (title, $id last segment, or fallback)
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

        // Select schema matching the declared read model name if available
        if (this.readModelSchemas && this.readModelSchemas.length > 0) {
            const matched = this.readModelSchemas.find((s, i) => getSchemaName(s, i) === readModelLine);
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

            // Handle child collection blocks: e.g., "orders=[" followed by inner statements and "]"
            const childStartMatch = statement.match(/^(\w+)\s*=\s*\[$/);
            if (childStartMatch) {
                const parentProp = childStartMatch[1];

                // consume inner block lines until we find a line with ']' as the statement
                let foundClosing = false;
                for (let j = i + 1; j < lines.length; j++) {
                    const innerRaw = lines[j].trim();
                    if (!innerRaw || innerRaw.startsWith('#')) continue;

                    if (!innerRaw.startsWith('|')) {
                        markers.push({
                            severity: 8,
                            startLineNumber: j + 1,
                            startColumn: 1,
                            endLineNumber: j + 1,
                            endColumn: Math.max(1, innerRaw.length + 1),
                            message: 'Statements inside a collection must start with |',
                        });
                        continue;
                    }

                    const innerStmt = innerRaw.substring(1).trim();
                    if (innerStmt === ']') {
                        foundClosing = true;
                        i = j; // advance outer loop to the closing line
                        break;
                    }

                    // validate child-specific statements (identifier etc.) or fall back to regular validation
                    if (!this.validateChildStatement(innerStmt, j + 1, markers, parentProp)) {
                        // if not a child-specific statement, validate as a normal statement
                        this.validateStatement(innerStmt, j + 1, markers);
                    }
                }

                if (!foundClosing) {
                    markers.push({
                        severity: 8,
                        startLineNumber: i + 1,
                        startColumn: 1,
                        endLineNumber: i + 1,
                        endColumn: statement.length + 1,
                        message: `Missing closing ']' for collection starting at '${parentProp}=['`,
                    });
                }

                continue;
            }

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
            const addMatch = statement.match(/^(\w+)\s*\+=\s*(\w+)\.(\w+)$/);
            const subtractMatch = statement.match(/^(\w+)\s*-=\s*(\w+)\.(\w+)$/);

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

                // Validate source is numeric too (e.g., OrderCompleted.amount)
                if (match[2] && match[3]) {
                    const eventName = match[2];
                    const eventSchema = this.eventSchemas[eventName];
                    const src = eventSchema?.properties?.[sourceProperty];
                    if (src && !this.isNumericType(src as any)) {
                        markers.push({
                            severity: 8,
                            startLineNumber: lineNumber,
                            startColumn: 1,
                            endLineNumber: lineNumber,
                            endColumn: statement.length + 1,
                            message: `Source property '${eventName}.${sourceProperty}' is not numeric (type='${src.type}').`,
                        });
                    }
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

                // Support forms: 'prop increment by 1', 'prop increment by Event.prop', or 'prop increment by EventType'
                const incMatchExtended = statement.match(/^([\w]+)\s+(?:increment|decrement)\s+by\s+(?:(\d+)|(\w+)\.(\w+)|(\w+))$/);
                if (incMatchExtended) {
                    const numberLiteral = incMatchExtended[2];
                    const eventName = incMatchExtended[3];
                    const eventProp = incMatchExtended[4];
                    // const identifier = incMatchExtended[5]; // event type occurrence

                    if (eventName && eventProp) {
                        const eventSchema = this.eventSchemas[eventName];
                        const src = eventSchema?.properties?.[eventProp];
                        if (src && !this.isNumericType(src as any)) {
                            markers.push({
                                severity: 8,
                                startLineNumber: lineNumber,
                                startColumn: 1,
                                endLineNumber: lineNumber,
                                endColumn: statement.length + 1,
                                message: `Cannot increment/decrement by '${eventName}.${eventProp}' because it is not numeric (type='${src.type}').`,
                            });
                        }
                    }
                    // numberLiteral is allowed; event type occurrence (identifier) is also allowed
                }
            }

            // Check property assignments for type compatibility
            const assignMatch = statement.match(/^(\w+)=(?:"([^"]*)"|(\d+)|(\w+)\.(\w+)|\$eventContext\.(\w+))$/);
            if (assignMatch) {
                const targetProperty = assignMatch[1];
                const stringLiteral = assignMatch[2];
                const numberLiteral = assignMatch[3];
                const eventName = assignMatch[4];
                const eventProp = assignMatch[5];
                const eventContextProp = assignMatch[6];

                const targetSchema = this.schema?.properties[targetProperty];
                if (!targetSchema) return;

                // Assignment from string literal
                if (stringLiteral !== undefined) {
                    if (targetSchema.type !== 'string') {
                        markers.push({
                            severity: 8,
                            startLineNumber: lineNumber,
                            startColumn: 1,
                            endLineNumber: lineNumber,
                            endColumn: statement.length + 1,
                            message: `Cannot assign string literal to property '${targetProperty}' of type '${targetSchema.type}'.`,
                        });
                    }
                    return;
                }

                // Assignment from number literal
                if (numberLiteral !== undefined) {
                    if (!this.isNumericType(targetSchema)) {
                        markers.push({
                            severity: 8,
                            startLineNumber: lineNumber,
                            startColumn: 1,
                            endLineNumber: lineNumber,
                            endColumn: statement.length + 1,
                            message: `Cannot assign numeric literal to property '${targetProperty}' of type '${targetSchema.type}'.`,
                        });
                    }
                    return;
                }

                // Assignment from event property
                if (eventName && eventProp) {
                    const evSchema = this.eventSchemas[eventName];
                    const src = evSchema?.properties?.[eventProp];
                    if (!src) {
                        markers.push({
                            severity: 4, // Warning
                            startLineNumber: lineNumber,
                            startColumn: 1,
                            endLineNumber: lineNumber,
                            endColumn: statement.length + 1,
                            message: `Unknown event property '${eventName}.${eventProp}'.`,
                        });
                        return;
                    }

                    // Type compatibility: numeric vs numeric, string vs string, otherwise mismatch
                    const bothNumeric = this.isNumericType(targetSchema) && this.isNumericType(src as any);
                    if (!bothNumeric && targetSchema.type !== src.type) {
                        markers.push({
                            severity: 8,
                            startLineNumber: lineNumber,
                            startColumn: 1,
                            endLineNumber: lineNumber,
                            endColumn: statement.length + 1,
                            message: `Type mismatch: cannot assign '${eventName}.${eventProp}' (type='${src.type}') to '${targetProperty}' (type='${targetSchema.type}').`,
                        });
                    }
                    return;
                }

                // Assignment from $eventContext
                if (eventContextProp) {
                    // basic mapping for $eventContext properties
                    const ctxTypeMap: Record<string, string> = {
                        occurred: 'string',
                        eventSourceId: 'string',
                        causedBy: 'string',
                        namespace: 'string',
                    };
                    const ctxType = ctxTypeMap[eventContextProp] || 'string';
                    if (targetSchema.type !== ctxType && !(this.isNumericType(targetSchema) && ctxType === 'number')) {
                        markers.push({
                            severity: 8,
                            startLineNumber: lineNumber,
                            startColumn: 1,
                            endLineNumber: lineNumber,
                            endColumn: statement.length + 1,
                            message: `Type mismatch: cannot assign '$eventContext.${eventContextProp}' to '${targetProperty}' (type='${targetSchema.type}').`,
                        });
                    }
                    return;
                }
            }
        }
    }

    // Returns true if the statement was recognized and handled as a child-specific directive
    private validateChildStatement(
        statement: string,
        lineNumber: number,
        markers: languages.IMarkerData[],
        parentProperty: string
    ): boolean {
        if (!statement) return false;

        // Support new syntax: "<propertyName> identifier" (e.g., "orderId identifier")
        const identifierPropMatch = statement.match(/^(\w+)\s+identifier$/);
        if (identifierPropMatch) {
            const idProp = identifierPropMatch[1];

            const parentSchema = this.schema?.properties?.[parentProperty];
            const itemsSchema = (parentSchema && (parentSchema as any).items) as JsonSchema | undefined;

            if (!parentSchema || !itemsSchema || !(itemsSchema as any).properties) {
                markers.push({
                    severity: 8,
                    startLineNumber: lineNumber,
                    startColumn: 1,
                    endLineNumber: lineNumber,
                    endColumn: statement.length + 1,
                    message: `Parent property '${parentProperty}' is not an array or missing item schema for child definitions.`,
                });
                return true;
            }

            const itemProps = (itemsSchema as any).properties as Record<string, any>;
            if (!itemProps[idProp]) {
                markers.push({
                    severity: 4, // Warning
                    startLineNumber: lineNumber,
                    startColumn: 1,
                    endLineNumber: lineNumber,
                    endColumn: statement.length + 1,
                    message: `Unknown identifier property '${idProp}' for items of '${parentProperty}'.`,
                });
            }

            return true;
        }

        return false;
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
    private schema: JsonSchema | undefined;
    private readModelSchemas: JsonSchema[] = [];
    private eventSchemas: Record<string, JsonSchema> = {};

    setSchema(schema: JsonSchema): void {
        // Backwards compat
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

        // If an array is provided, normalize to a map keyed by derived name (title/name/$id)
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
        let activeSchema: JsonSchema | undefined = this.schema as any;
        if (this.readModelSchemas && this.readModelSchemas.length > 0) {
            const matched = this.readModelSchemas.find((s, i) => getSchemaName(s, i) === declaredReadModel);
            if (matched) activeSchema = matched as any;
            else if (!declaredReadModel && this.readModelSchemas.length === 1) activeSchema = this.readModelSchemas[0] as any;
        }

        // If we are editing the first line, suggest available read model names
        if (position.lineNumber === 1 && this.readModelSchemas && this.readModelSchemas.length > 0) {
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

            return { suggestions };
        }

        // If at the start of a new line after first line, suggest |
        if (position.lineNumber > 1 && currentLine.trim() === '') {
            suggestions.push({
                label: '|',
                kind: 24, // Operator
                insertText: '| ',
                documentation: 'Start a new statement',
                range: this.getWordRange(model, position),
            });
        }

        // After |, provide token-aware suggestions:
        // - First token (immediately after |): property names only
        // - Second token (operator): operators filtered by property type
        // - Third token (RHS): event type names, $eventContext, literals
        if (currentLine.trim().startsWith('|')) {
            const pipeIndex = currentLine.indexOf('|');
            const partUpToCursor = currentLine.substring(0, position.column - 1);
            const afterPipeUpToCursor = partUpToCursor.substring(pipeIndex + 1);
            const afterPipeTrimmed = afterPipeUpToCursor.replace(/^\s+/, '');
            const tokens = afterPipeTrimmed.length === 0 ? [] : afterPipeTrimmed.split(/\s+/).filter(Boolean);
            const lastCharBeforeCursor = partUpToCursor.length > 0 ? partUpToCursor.charAt(partUpToCursor.length - 1) : ' ';
            const isAtTokenStart = /\s/.test(lastCharBeforeCursor);

            // Helper: propose property names
            const suggestProperties = () => {
                if (!activeSchema || !activeSchema.properties) return;
                Object.keys(activeSchema.properties).forEach((propName) => {
                    const propSchema = activeSchema!.properties[propName];
                    suggestions.push({
                        label: propName,
                        kind: 9, // Property
                        insertText: propName,
                        documentation: `Property: ${propName} (${propSchema.type}${propSchema.format ? `:${propSchema.format}` : ''})`,
                        range: this.getWordRange(model, position),
                    });
                });
            };

            // Helper: propose operators for a given property name
            const suggestOperatorsFor = (propName: string) => {
                const propSchema = activeSchema?.properties?.[propName];
                // Always suggest assignment
                suggestions.push({
                    label: '=',
                    kind: 24,
                    insertText: '=',
                    documentation: 'Set property value',
                    range: this.getWordRange(model, position),
                });

                if (propSchema && this.isNumericType(propSchema as any)) {
                    suggestions.push({
                        label: '+=',
                        kind: 24,
                        insertText: '+=',
                        documentation: 'Add to property value',
                        range: this.getWordRange(model, position),
                    });
                    suggestions.push({
                        label: '-=',
                        kind: 24,
                        insertText: '-=',
                        documentation: 'Subtract from property value',
                        range: this.getWordRange(model, position),
                    });
                    suggestions.push({
                        label: 'increment',
                        kind: 14,
                        insertText: 'increment',
                        documentation: 'Increment the property',
                        range: this.getWordRange(model, position),
                    });
                    suggestions.push({
                        label: 'decrement',
                        kind: 14,
                        insertText: 'decrement',
                        documentation: 'Decrement the property',
                        range: this.getWordRange(model, position),
                    });
                }
            };

            // Decide which token we're completing
            if (tokens.length === 0 || (tokens.length === 1 && !isAtTokenStart)) {
                // First token (property name)
                suggestProperties();
                return { suggestions };
            }

            if ((tokens.length === 1 && isAtTokenStart) || (tokens.length === 2 && !isAtTokenStart)) {
                // Second token (operator) — determine property name (first token)
                const firstToken = tokens[0];
                suggestOperatorsFor(firstToken);
                return { suggestions };
            }

            // Third token or later: RHS suggestions (event types, $eventContext, literals)
            // If we're completing after an event type and a dot, suggest that event's properties
            const eventPropMatchCursor = afterPipeTrimmed.match(/(\w+)\.(\w*)$/);
            if (eventPropMatchCursor) {
                const evName = eventPropMatchCursor[1];
                const typedProp = eventPropMatchCursor[2] || '';
                const evSchema = this.eventSchemas[evName];
                if (evSchema && evSchema.properties) {
                    Object.keys(evSchema.properties).forEach((propName) => {
                        if (!typedProp || propName.startsWith(typedProp)) {
                            const prop = evSchema.properties![propName];
                            suggestions.push({
                                label: propName,
                                kind: 9, // Property
                                insertText: propName,
                                documentation: `Event property: ${propName} (${prop.type}${prop.format ? `:${prop.format}` : ''})`,
                                range: this.getWordRange(model, position),
                            });
                        }
                    });
                    return { suggestions };
                }
            }

            // Suggest event type names
            const word = model.getWordUntilPosition(position);
            Object.keys(this.eventSchemas).forEach((eventName) => {
                if (!word.word || eventName.startsWith(word.word)) {
                    suggestions.push({
                        label: eventName,
                        kind: 6, // Class
                        insertText: eventName,
                        documentation: `Event type: ${eventName}`,
                        range: this.getWordRange(model, position),
                    });
                }
            });

            // Suggest $eventContext
            suggestions.push({
                label: '$eventContext',
                kind: 14,
                insertText: '$eventContext.',
                documentation: 'Access event context properties',
                range: this.getWordRange(model, position),
            });

            // Also suggest a numeric literal placeholder for increment/decrement
            suggestions.push({
                label: '1',
                kind: 14,
                insertText: '1',
                documentation: 'Numeric literal',
                range: this.getWordRange(model, position),
            });

            return { suggestions };
        }

        // Suggest event type names when typing right-hand side before a dot
        const rhsMatch = currentLine.match(/=\s*(\w*)$/);
        if (rhsMatch && rhsMatch[1] !== undefined) {
            const typed = rhsMatch[1];
            Object.keys(this.eventSchemas).forEach((eventName) => {
                if (!typed || eventName.startsWith(typed)) {
                    suggestions.push({
                        label: eventName,
                        kind: 6, // Class
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
                        kind: 9, // Property
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
                            kind: 9,
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
