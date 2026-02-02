// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor, languages, IRange, Position } from 'monaco-editor';
import type { JsonSchema, JsonSchemaProperty } from '../JsonSchema';
import type { ReadModelInfo } from './index';

interface CompletionContext {
    lineContent: string;
    textBeforeCursor: string;
    textAfterCursor: string;
    currentWord: string;
    indentLevel: number;
    lineNumber: number;
}

export class ProjectionDefinitionLanguageCompletionProvider implements languages.CompletionItemProvider {
    private readModels: ReadModelInfo[] = [];
    private eventSchemas: Record<string, JsonSchema> = {};
    private eventSequences: string[] = [];

    setReadModels(readModels: ReadModelInfo[]): void {
        this.readModels = readModels || [];
    }

    setEventSequences(sequences: string[]): void {
        this.eventSequences = sequences || [];
    }

    // Keep for backwards compatibility
    setReadModelSchemas(schemas: JsonSchema[]): void {
        this.readModels = schemas.map(schema => ({
            displayName: this.getSchemaName(schema) || '',
            schema
        }));
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
                const name = this.getSchemaName(s) || `Event${i + 1}`;
                map[name] = s;
            });
            this.eventSchemas = map;
            return;
        }

        this.eventSchemas = schemas;
    }

    provideCompletionItems(
        model: editor.ITextModel,
        position: Position
    ): languages.ProviderResult<languages.CompletionList> {
        const context = this.buildContext(model, position);
        const suggestions: languages.CompletionItem[] = [];

        // Get the active read model schema from the projection declaration
        const activeSchema = this.getActiveReadModelSchema(model);

        // Determine what kind of completions to provide based on context
        if (context.lineNumber === 1) {
            this.addProjectionLineCompletions(suggestions, context);
        } else if (this.isAfterKeyword(context, 'sequence')) {
            this.addEventSequenceCompletions(suggestions, context);
        } else if (this.isAfterKeyword(context, 'from') || this.isAfterKeyword(context, 'every')) {
            this.addEventTypeCompletions(suggestions, context);
        } else if (this.isAfterKeyword(context, 'key')) {
            this.addKeyCompletions(suggestions, context, activeSchema);
        } else if (this.isAfterKeyword(context, 'parent')) {
            this.addParentCompletions(suggestions, context);
        } else if (this.isAfterKeyword(context, 'children')) {
            this.addChildrenCompletions(suggestions, context, activeSchema);
        } else if (this.isAfterKeyword(context, 'join')) {
            this.addJoinCompletions(suggestions, context, activeSchema);
        } else if (this.isAfterKeyword(context, 'with')) {
            this.addEventTypeCompletions(suggestions, context);
        } else if (this.isAfterNumericOperationKeyword(context)) {
            this.addNumericPropertyCompletions(suggestions, context, activeSchema);
        } else if (this.isAfterAddOrSubtractBy(context)) {
            this.addAssignmentValueCompletions(suggestions, context, model, position);
        } else if (this.isInAssignment(context)) {
            this.addAssignmentValueCompletions(suggestions, context, model, position);
        } else if (this.isPropertyDotAccess(context)) {
            this.addPropertyMemberCompletions(suggestions, context);
        } else if (this.isStartOfLine(context) || this.isAfterIndent(context)) {
            this.addStatementCompletions(suggestions, context, activeSchema);
        }

        return { suggestions };
    }

    private buildContext(model: editor.ITextModel, position: Position): CompletionContext {
        const lineContent = model.getLineContent(position.lineNumber);
        const textBeforeCursor = lineContent.substring(0, position.column - 1);
        const textAfterCursor = lineContent.substring(position.column - 1);
        const word = model.getWordAtPosition(position);
        const currentWord = word?.word || '';
        const indentLevel = this.getIndentLevel(textBeforeCursor);

        return {
            lineContent,
            textBeforeCursor,
            textAfterCursor,
            currentWord,
            indentLevel,
            lineNumber: position.lineNumber,
        };
    }

    private getIndentLevel(text: string): number {
        const match = text.match(/^(\s*)/);
        return match ? Math.floor(match[1].length / 2) : 0;
    }

    private isStartOfLine(context: CompletionContext): boolean {
        return context.textBeforeCursor.trim() === '';
    }

    private isAfterIndent(context: CompletionContext): boolean {
        return /^\s+$/.test(context.textBeforeCursor);
    }

    private isAfterKeyword(context: CompletionContext, keyword: string): boolean {
        const pattern = new RegExp(`\\b${keyword}\\s+\\w*$`);
        return pattern.test(context.textBeforeCursor);
    }

    private isAfterNumericOperationKeyword(context: CompletionContext): boolean {
        // Match: count, increment, decrement, add, subtract followed by optional word (but not "by")
        // For add/subtract, we only match if "by" hasn't been typed yet
        const countIncrDecrPattern = /\b(count|increment|decrement)\s+\w*$/;
        const addSubtractPattern = /\b(add|subtract)\s+\w*$/;

        // Check for count/increment/decrement first
        if (countIncrDecrPattern.test(context.textBeforeCursor)) {
            return true;
        }

        // For add/subtract, only match if "by" hasn't been typed yet
        if (addSubtractPattern.test(context.textBeforeCursor)) {
            // Make sure we're not after "by"
            return !this.isAfterAddOrSubtractBy(context);
        }

        return false;
    }

    private isAfterAddOrSubtractBy(context: CompletionContext): boolean {
        // Match: "add <property> by " or "subtract <property> by " followed by optional word
        const pattern = /\b(add|subtract)\s+\w+\s+by\s+\w*$/;
        return pattern.test(context.textBeforeCursor);
    }

    private isInAssignment(context: CompletionContext): boolean {
        return /\w+\s*=\s*\w*$/.test(context.textBeforeCursor);
    }

    private isPropertyDotAccess(context: CompletionContext): boolean {
        return /(\w+|\$\w+)\.(\w*)$/.test(context.textBeforeCursor);
    }

    private addProjectionLineCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        const trimmed = context.textBeforeCursor.trim();

        // Suggest "projection" keyword
        if (trimmed === '' || 'projection'.startsWith(trimmed)) {
            suggestions.push({
                label: 'projection',
                kind: 14, // Keyword
                insertText: 'projection ',
                documentation: 'Define a projection that transforms events into a read model',
                detail: 'projection <ProjectionName>',
                range: this.getRangeForWord(context),
            });
        }

        // Check if cursor is after "=>" to suggest read model names
        const arrowMatch = trimmed.match(/^projection\s+[\w.]+\s*=>\s*([\w.]*)$/);
        if (arrowMatch) {
            const partialReadModel = arrowMatch[1] || '';
            this.readModels.forEach((readModel) => {
                if (!partialReadModel || readModel.displayName.toLowerCase().startsWith(partialReadModel.toLowerCase())) {
                    suggestions.push({
                        label: readModel.displayName,
                        kind: 6, // Class
                        insertText: readModel.displayName,
                        documentation: `Read model: ${readModel.displayName}`,
                        detail: this.getSchemaDescription(readModel.schema),
                        range: this.getRangeForWord(context),
                    });
                }
            });
            return;
        }

        // After "projection ", suggest read model names (for quick completion of both projection name and read model)
        if (trimmed.startsWith('projection ') && !trimmed.includes('=>')) {
            const afterProjection = trimmed.substring('projection '.length);
            this.readModels.forEach((readModel) => {
                if (!afterProjection || readModel.displayName.toLowerCase().startsWith(afterProjection.toLowerCase())) {
                    suggestions.push({
                        label: readModel.displayName,
                        kind: 6, // Class
                        insertText: `${readModel.displayName} => ${readModel.displayName}`,
                        documentation: `Read model: ${readModel.displayName}`,
                        detail: this.getSchemaDescription(readModel.schema),
                        range: this.getRangeForWord(context),
                    });
                }
            });
        }
    }

    private addEventTypeCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        Object.keys(this.eventSchemas).forEach((eventName) => {
            if (!context.currentWord || eventName.startsWith(context.currentWord)) {
                const schema = this.eventSchemas[eventName];
                suggestions.push({
                    label: eventName,
                    kind: 6, // Class
                    insertText: eventName,
                    documentation: this.getSchemaDescription(schema) || `Event type: ${eventName}`,
                    detail: 'Event Type',
                    range: this.getRangeForWord(context),
                });
            }
        });
    }

    private addEventSequenceCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        this.eventSequences.forEach((sequenceId) => {
            if (!context.currentWord || sequenceId.startsWith(context.currentWord)) {
                suggestions.push({
                    label: sequenceId,
                    kind: 13, // Value
                    insertText: sequenceId,
                    documentation: `Event sequence: ${sequenceId}`,
                    detail: 'Event Sequence',
                    range: this.getRangeForWord(context),
                });
            }
        });
    }

    private addKeyCompletions(suggestions: languages.CompletionItem[], context: CompletionContext, activeSchema?: JsonSchema): void {
        // Check if this might be a composite key (type reference)
        if (activeSchema?.definitions) {
            Object.keys(activeSchema.definitions).forEach((typeName) => {
                const typeDef = activeSchema.definitions![typeName];
                if (typeDef.type === 'object') {
                    suggestions.push({
                        label: typeName,
                        kind: 6, // Class
                        insertText: typeName,
                        documentation: `Composite key type: ${typeName}`,
                        detail: 'Composite Key Type',
                        range: this.getRangeForWord(context),
                    });
                }
            });
        }

        // Suggest built-in expressions for simple keys
        this.addExpressionCompletions(suggestions, context);
    }

    private addParentCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        this.addExpressionCompletions(suggestions, context);
    }

    private addNumericPropertyCompletions(suggestions: languages.CompletionItem[], context: CompletionContext, activeSchema?: JsonSchema): void {
        // After "count ", "increment ", or "decrement ", suggest numeric properties from read model
        if (activeSchema?.properties) {
            Object.entries(activeSchema.properties).forEach(([propName, propSchema]) => {
                // Suggest numeric properties (integer or number types)
                const isNumeric = propSchema.type === 'integer' || propSchema.type === 'number';
                if (isNumeric) {
                    if (!context.currentWord || propName.startsWith(context.currentWord)) {
                        suggestions.push({
                            label: propName,
                            kind: 9, // Property
                            insertText: propName,
                            documentation: `Numeric property: ${propName}`,
                            detail: this.getPropertyTypeDescription(propSchema),
                            range: this.getRangeForWord(context),
                        });
                    }
                }
            });
        }

        // Also check if "add" or "subtract" is being typed - need to handle "add <prop> by" pattern
        const addSubtractMatch = context.textBeforeCursor.match(/\b(add|subtract)\s+(\w*)$/);
        if (addSubtractMatch) {
            const partialProp = addSubtractMatch[2] || '';
            if (activeSchema?.properties) {
                Object.entries(activeSchema.properties).forEach(([propName, propSchema]) => {
                    const isNumeric = propSchema.type === 'integer' || propSchema.type === 'number';
                    if (isNumeric && (!partialProp || propName.startsWith(partialProp))) {
                        suggestions.push({
                            label: propName,
                            kind: 9, // Property
                            insertText: `${propName} by `,
                            documentation: `Numeric property: ${propName}`,
                            detail: this.getPropertyTypeDescription(propSchema),
                            range: this.getRangeForWord(context),
                        });
                    }
                });
            }
        }
    }

    private addChildrenCompletions(suggestions: languages.CompletionItem[], context: CompletionContext, activeSchema?: JsonSchema): void {
        // After "children ", suggest collection properties from read model
        if (activeSchema?.properties) {
            Object.entries(activeSchema.properties).forEach(([propName, propSchema]) => {
                // Collections are typically arrays
                if (propSchema.type === 'array' || propSchema.items) {
                    suggestions.push({
                        label: propName,
                        kind: 9, // Property
                        insertText: `${propName} id `,
                        documentation: `Collection property: ${propName}`,
                        detail: this.getPropertyTypeDescription(propSchema),
                        range: this.getRangeForWord(context),
                    });
                }
            });
        }
    }

    private addJoinCompletions(suggestions: languages.CompletionItem[], context: CompletionContext, activeSchema?: JsonSchema): void {
        const trimmed = context.textBeforeCursor.trim();

        if (trimmed === 'join' || !trimmed.includes(' ')) {
            // Suggest collection properties
            if (activeSchema?.properties) {
                Object.entries(activeSchema.properties).forEach(([propName, propSchema]) => {
                    if (propSchema.type === 'array' || propSchema.items) {
                        suggestions.push({
                            label: propName,
                            kind: 9,
                            insertText: `${propName} on `,
                            documentation: `Join on collection: ${propName}`,
                            range: this.getRangeForWord(context),
                        });
                    }
                });
            }
        } else if (trimmed.match(/join\s+\w+\s+on\s+/)) {
            // After "on", suggest properties to join on
            if (activeSchema?.properties) {
                Object.keys(activeSchema.properties).forEach((propName) => {
                    suggestions.push({
                        label: propName,
                        kind: 9,
                        insertText: propName,
                        documentation: `Property: ${propName}`,
                        range: this.getRangeForWord(context),
                    });
                });
            }
        }
    }

    private addStatementCompletions(suggestions: languages.CompletionItem[], context: CompletionContext, activeSchema?: JsonSchema): void {
        // Add all statement keywords
        const keywords = [
            { label: 'sequence', insertText: 'sequence ', documentation: 'Specify which event sequence to use for this projection', detail: 'sequence <event-sequence-id>' },
            { label: 'from', insertText: 'from ', documentation: 'Handle specific event types', detail: 'from <EventType>' },
            { label: 'every', insertText: 'every', documentation: 'Handle all events for automatic mapping', detail: 'every' },
            { label: 'key', insertText: 'key ', documentation: 'Define the projection key', detail: 'key <expression>' },
            { label: 'parent', insertText: 'parent ', documentation: 'Define parent relationship', detail: 'parent <expression>' },
            { label: 'join', insertText: 'join ', documentation: 'Join with child collection', detail: 'join <collection> on <property>' },
            { label: 'children', insertText: 'children ', documentation: 'Define child collection operations', detail: 'children <collection> identified by <expression>' },
            { label: 'remove with', insertText: 'remove with ', documentation: 'Remove items based on event', detail: 'remove with <EventType>' },
            { label: 'remove via', insertText: 'remove via join on ', documentation: 'Remove items via join', detail: 'remove via join on <EventType>' },
            { label: 'no automap', insertText: 'no automap', documentation: 'Disable automatic property mapping', detail: 'no automap' },
            { label: 'increment', insertText: 'increment ', documentation: 'Increment a numeric property', detail: 'increment <property>' },
            { label: 'decrement', insertText: 'decrement ', documentation: 'Decrement a numeric property', detail: 'decrement <property>' },
            { label: 'count', insertText: 'count ', documentation: 'Count occurrences', detail: 'count <property>' },
            { label: 'add', insertText: 'add ', documentation: 'Add value to property', detail: 'add <property> by <expression>' },
            { label: 'subtract', insertText: 'subtract ', documentation: 'Subtract value from property', detail: 'subtract <property> by <expression>' },
            { label: 'exclude children', insertText: 'exclude children', documentation: 'Exclude child collections from automap', detail: 'exclude children' },
        ];

        keywords.forEach((kw) => {
            if (!context.currentWord || kw.label.startsWith(context.currentWord)) {
                suggestions.push({
                    label: kw.label,
                    kind: 14, // Keyword
                    insertText: kw.insertText,
                    documentation: kw.documentation,
                    detail: kw.detail,
                    range: this.getRangeForWord(context),
                });
            }
        });

        // Add property assignments
        if (activeSchema?.properties) {
            Object.entries(activeSchema.properties).forEach(([propName, propSchema]) => {
                if (!context.currentWord || propName.startsWith(context.currentWord)) {
                    suggestions.push({
                        label: propName,
                        kind: 9, // Property
                        insertText: `${propName} = `,
                        documentation: `Assign value to property: ${propName}`,
                        detail: this.getPropertyTypeDescription(propSchema),
                        range: this.getRangeForWord(context),
                    });
                }
            });
        }
    }

    private addAssignmentValueCompletions(
        suggestions: languages.CompletionItem[],
        context: CompletionContext,
        model: editor.ITextModel,
        position: Position
    ): void {
        // Get the event type from the current block
        const eventType = this.getCurrentEventType(model, position.lineNumber);

        // Suggest event properties if we know the event type
        if (eventType && this.eventSchemas[eventType]) {
            const eventSchema = this.eventSchemas[eventType];
            if (eventSchema.properties) {
                Object.entries(eventSchema.properties).forEach(([propName, propSchema]) => {
                    suggestions.push({
                        label: propName,
                        kind: 9,
                        insertText: propName,
                        documentation: `Event property: ${propName}`,
                        detail: this.getPropertyTypeDescription(propSchema),
                        range: this.getRangeForWord(context),
                    });
                });
            }
        }

        // Add expression completions
        this.addExpressionCompletions(suggestions, context);
    }

    private addExpressionCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        const expressions = [
            {
                label: '$eventSourceId',
                insertText: '$eventSourceId',
                documentation: 'The identifier of the event source (aggregate/entity)',
                detail: 'Event Source Expression'
            },
            {
                label: '$causedBy',
                insertText: '$causedBy',
                documentation: 'Information about who/what caused the event',
                detail: 'Identity Expression',
                children: [
                    { name: 'subject', doc: 'The subject identifier' },
                    { name: 'name', doc: 'The display name' },
                    { name: 'userName', doc: 'The username' },
                ]
            },
            {
                label: '$eventContext',
                insertText: '$eventContext.',
                documentation: 'Access event context properties',
                detail: 'Event Context Expression',
                children: [
                    { name: 'sequenceNumber', doc: 'The event sequence number' },
                    { name: 'occurred', doc: 'When the event occurred' },
                    { name: 'eventSourceId', doc: 'The event source identifier' },
                    { name: 'namespace', doc: 'The event namespace' },
                ]
            },
            {
                label: '$occurred',
                insertText: '$occurred',
                documentation: 'Timestamp when the event occurred',
                detail: 'Occurred Expression'
            },
            {
                label: '$namespace',
                insertText: '$namespace',
                documentation: 'The namespace of the event',
                detail: 'Namespace Expression'
            },
        ];

        expressions.forEach((expr) => {
            if (!context.currentWord || expr.label.startsWith(context.currentWord) || expr.label.startsWith('$' + context.currentWord)) {
                suggestions.push({
                    label: expr.label,
                    kind: 14, // Keyword/builtin
                    insertText: expr.insertText,
                    documentation: expr.documentation,
                    detail: expr.detail,
                    range: this.getRangeForWord(context),
                });
            }
        });
    }

    private addPropertyMemberCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        const match = context.textBeforeCursor.match(/(\w+|\$\w+)\.(\w*)$/);
        if (!match) return;

        const [, objectName, partialProp] = match;

        // Handle $causedBy properties
        if (objectName === '$causedBy') {
            const causedByProps = [
                { name: 'subject', doc: 'The subject identifier', type: 'string' },
                { name: 'name', doc: 'The display name', type: 'string' },
                { name: 'userName', doc: 'The username', type: 'string' },
            ];

            causedByProps.forEach((prop) => {
                if (!partialProp || prop.name.startsWith(partialProp)) {
                    suggestions.push({
                        label: prop.name,
                        kind: 9,
                        insertText: prop.name,
                        documentation: prop.doc,
                        detail: prop.type,
                        range: this.getRangeForWord(context),
                    });
                }
            });
            return;
        }

        // Handle $eventContext properties
        if (objectName === '$eventContext') {
            const contextProps = [
                { name: 'sequenceNumber', doc: 'The event sequence number', type: 'EventSequenceNumber' },
                { name: 'occurred', doc: 'When the event occurred', type: 'DateTimeOffset' },
                { name: 'eventSourceId', doc: 'The event source identifier', type: 'string' },
                { name: 'namespace', doc: 'The event namespace', type: 'string' },
            ];

            contextProps.forEach((prop) => {
                if (!partialProp || prop.name.startsWith(partialProp)) {
                    suggestions.push({
                        label: prop.name,
                        kind: 9,
                        insertText: prop.name,
                        documentation: prop.doc,
                        detail: prop.type,
                        range: this.getRangeForWord(context),
                    });
                }
            });
            return;
        }

        // Handle event type properties
        const eventSchema = this.eventSchemas[objectName];
        if (eventSchema?.properties) {
            Object.entries(eventSchema.properties).forEach(([propName, propSchema]) => {
                if (!partialProp || propName.startsWith(partialProp)) {
                    suggestions.push({
                        label: propName,
                        kind: 9,
                        insertText: propName,
                        documentation: `Event property: ${propName}`,
                        detail: this.getPropertyTypeDescription(propSchema),
                        range: this.getRangeForWord(context),
                    });
                }
            });
        }
    }

    private getCurrentEventType(model: editor.ITextModel, lineNumber: number): string | null {
        // Search backwards to find the event type declaration
        for (let i = lineNumber; i >= 1; i--) {
            const line = model.getLineContent(i).trim();
            const fromMatch = line.match(/^from\s+(\w+)/);
            if (fromMatch) {
                return fromMatch[1];
            }
            const everyMatch = line.match(/^every$/);
            if (everyMatch) {
                return null; // every block doesn't have a specific event type
            }
        }
        return null;
    }

    private getActiveReadModelSchema(model: editor.ITextModel): JsonSchema | undefined {
        const firstLine = model.getLineContent(1).trim();
        const match = firstLine.match(/^projection\s+\w+\s*=>\s*(\w+)/);
        if (!match) return undefined;

        const readModelName = match[1];
        const readModel = this.readModels.find((rm) => rm.displayName === readModelName);
        return readModel?.schema;
    }

    private getSchemaName(schema: JsonSchema): string | undefined {
        if (schema.name) return schema.name;
        if (schema.title) return schema.title;
        if (typeof schema.$id === 'string') {
            const parts = schema.$id.split('/');
            return parts[parts.length - 1] || schema.$id;
        }
        return undefined;
    }

    private getSchemaDescription(schema: JsonSchema): string | undefined {
        return schema.description || undefined;
    }

    private getPropertyTypeDescription(propSchema: JsonSchemaProperty): string {
        if (propSchema.type === 'array' && propSchema.items) {
            const itemType = propSchema.items.type || propSchema.items.$id?.split('/').pop() || 'unknown';
            return `${itemType}[]`;
        }
        return propSchema.type || propSchema.$ref?.split('/').pop() || 'unknown';
    }

    private getRangeForWord(context: CompletionContext): IRange {
        const startColumn = context.textBeforeCursor.length - context.currentWord.length + 1;
        const endColumn = startColumn + context.currentWord.length;
        return {
            startLineNumber: context.lineNumber,
            startColumn: Math.max(1, startColumn),
            endLineNumber: context.lineNumber,
            endColumn: Math.max(1, endColumn),
        };
    }
}
