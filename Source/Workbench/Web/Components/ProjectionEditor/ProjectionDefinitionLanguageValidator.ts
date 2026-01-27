// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor } from 'monaco-editor';
import type { JsonSchema } from '../JsonSchema';
import type { ReadModelInfo } from './index';

export class ProjectionDefinitionLanguageValidator {
    private readModels: ReadModelInfo[] = [];
    private eventSchemas: Record<string, JsonSchema> = {};

    setReadModels(readModels: ReadModelInfo[]): void {
        this.readModels = readModels || [];
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

        this.eventSchemas = schemas || {};
    }

    validate(model: editor.ITextModel): editor.IMarkerData[] {
        const markers: editor.IMarkerData[] = [];
        const content = model.getValue();

        // Skip validation if content is empty or only whitespace
        // This prevents spurious errors when the model is first created before content is loaded
        if (!content.trim()) {
            return markers;
        }

        // Normalize line endings and split
        const lines = content.replace(/\r\n/g, '\n').replace(/\r/g, '\n').split('\n');

        // Find the first non-empty, non-comment line
        let firstNonEmptyLineIndex = 0;
        let firstLine = '';
        for (let i = 0; i < lines.length; i++) {
            const trimmed = lines[i].trim();
            if (trimmed && !trimmed.startsWith('#')) {
                firstNonEmptyLineIndex = i;
                firstLine = trimmed;
                break;
            }
        }

        if (!firstLine) {
            // This should not happen since we already checked for empty content above,
            // but if it does (content is all comments), just return without error
            return markers;
        }

        // Validate first meaningful line - projection declaration
        const projectionMatch = firstLine.match(/^projection\s+(\w+)\s*=>\s*(\w+)\s*$/);

        if (!projectionMatch) {
            const lineNumber = firstNonEmptyLineIndex + 1;
            markers.push(this.createError(lineNumber, 1, firstLine.length + 1, 'Projection definition must start with "projection <ProjectionName> => <ReadModelName>"'));
            return markers;
        }

        const [, , readModelName] = projectionMatch;

        // Validate read model exists in schemas
        if (this.readModels.length > 0) {
            const readModelExists = this.readModels.some((rm) => rm.displayName === readModelName);
            if (!readModelExists) {
                const lineNumber = firstNonEmptyLineIndex + 1;
                const col = firstLine.indexOf(readModelName) + 1;
                markers.push(this.createError(lineNumber, col, col + readModelName.length, `Read model '${readModelName}' not found`));
            }
        }

        // Get the active read model schema for property validation
        const activeReadModel = this.readModels.find((rm) => rm.displayName === readModelName);
        const activeSchema = activeReadModel?.schema;

        // Validate subsequent lines
        const contextStack: JsonSchema[] = [activeSchema!];
        const indentStack: number[] = [0]; // Track indentation levels for context
        let lastIndent = 0;
        let hasFromStatements = false;

        for (let i = 1; i < lines.length; i++) {
            const lineNumber = i + 1;
            const line = lines[i];
            const trimmed = line.trim();

            if (!trimmed || trimmed.startsWith('#')) continue;

            // Calculate current indentation
            const currentIndent = line.search(/\S/);

            // Pop context when dedenting (exiting a children block)
            while (currentIndent < lastIndent && indentStack.length > 1) {
                const poppedIndent = indentStack.pop();
                if (poppedIndent !== undefined && currentIndent < poppedIndent) {
                    contextStack.pop();
                }
            }
            lastIndent = currentIndent;

            // Validate event types in from/every blocks
            const fromMatch = trimmed.match(/^from\s+(\w+)/);
            if (fromMatch) {
                hasFromStatements = true;
                const eventType = fromMatch[1];
                if (Object.keys(this.eventSchemas).length > 0 && !this.eventSchemas[eventType]) {
                    const col = line.indexOf(eventType) + 1;
                    markers.push(this.createWarning(lineNumber, col, col + eventType.length, `Event type '${eventType}' not found in available schemas`));
                }
            }

            // Validate event types in join/remove blocks
            const eventsMatch = trimmed.match(/^events\s+(\w+)/);
            if (eventsMatch) {
                const eventType = eventsMatch[1];
                if (Object.keys(this.eventSchemas).length > 0 && !this.eventSchemas[eventType]) {
                    const col = line.indexOf(eventType) + 1;
                    markers.push(this.createWarning(lineNumber, col, col + eventType.length, `Event type '${eventType}' not found in available schemas`));
                }
            }

            const removeWithMatch = trimmed.match(/^remove\s+(?:with|via\s+join\s+on)\s+(\w+)/);
            if (removeWithMatch) {
                const eventType = removeWithMatch[1];
                if (Object.keys(this.eventSchemas).length > 0 && !this.eventSchemas[eventType]) {
                    const col = line.indexOf(eventType) + 1;
                    markers.push(this.createWarning(lineNumber, col, col + eventType.length, `Event type '${eventType}' not found in available schemas`));
                }
            }

            // Validate property assignments
            const assignmentMatch = trimmed.match(/^(\w+)\s*=\s*(.+)$/);
            if (assignmentMatch) {
                const currentSchema = contextStack[contextStack.length - 1];
                if (currentSchema?.properties) {
                    const [, propertyName] = assignmentMatch;
                    if (!currentSchema.properties[propertyName]) {
                        const col = line.indexOf(propertyName) + 1;
                        const schemaName = this.getSchemaName(currentSchema) || 'current schema';
                        markers.push(this.createWarning(lineNumber, col, col + propertyName.length, `Property '${propertyName}' not found in ${schemaName}`));
                    }
                }
            }

            // Validate increment/decrement/count targets
            const numericOpMatch = trimmed.match(/^(?:increment|decrement|count)\s+(\w+)$/);
            if (numericOpMatch) {
                const currentSchema = contextStack[contextStack.length - 1];
                if (currentSchema?.properties) {
                    const [, propertyName] = numericOpMatch;
                    if (!currentSchema.properties[propertyName]) {
                        const col = line.indexOf(propertyName) + 1;
                        const schemaName = this.getSchemaName(currentSchema) || 'current schema';
                        markers.push(this.createWarning(lineNumber, col, col + propertyName.length, `Property '${propertyName}' not found in ${schemaName}`));
                    }
                }
            }

            // Validate add/subtract targets
            const addSubMatch = trimmed.match(/^(?:add|subtract)\s+(\w+)\s+by\s+/);
            if (addSubMatch) {
                const currentSchema = contextStack[contextStack.length - 1];
                if (currentSchema?.properties) {
                    const [, propertyName] = addSubMatch;
                    if (!currentSchema.properties[propertyName]) {
                        const col = line.indexOf(propertyName) + 1;
                        const schemaName = this.getSchemaName(currentSchema) || 'current schema';
                        markers.push(this.createWarning(lineNumber, col, col + propertyName.length, `Property '${propertyName}' not found in ${schemaName}`));
                    }
                }
            }

            // Validate children references
            const childrenMatch = trimmed.match(/^children\s+(\w+)\s+identified\s+by\s+/);
            if (childrenMatch) {
                const currentSchema = contextStack[contextStack.length - 1];
                if (currentSchema?.properties) {
                    const [, propertyName] = childrenMatch;
                    const prop = currentSchema.properties[propertyName];
                    if (!prop) {
                        const col = line.indexOf(propertyName) + 1;
                        const schemaName = this.getSchemaName(currentSchema) || 'current schema';
                        markers.push(this.createWarning(lineNumber, col, col + propertyName.length, `Collection property '${propertyName}' not found in ${schemaName}`));
                    } else if (prop.type !== 'array' && !prop.items) {
                        const col = line.indexOf(propertyName) + 1;
                        markers.push(this.createWarning(lineNumber, col, col + propertyName.length, `Property '${propertyName}' is not a collection (array) type`));
                    } else {
                        // Push child schema onto context stack for validating nested properties
                        const childSchema = prop.items;
                        if (childSchema) {
                            contextStack.push(childSchema);
                            indentStack.push(currentIndent);
                        }
                    }
                }
            }

            // Validate join references
            const joinMatch = trimmed.match(/^join\s+(\w+)\s+on\s+(\w+)/);
            if (joinMatch && activeSchema?.properties) {
                const [, collectionName, joinProperty] = joinMatch;
                const collectionProp = activeSchema.properties[collectionName];
                if (!collectionProp) {
                    const col = line.indexOf(collectionName) + 1;
                    markers.push(this.createWarning(lineNumber, col, col + collectionName.length, `Collection property '${collectionName}' not found in read model '${readModelName}'`));
                } else if (collectionProp.type !== 'array' && !collectionProp.items) {
                    const col = line.indexOf(collectionName) + 1;
                    markers.push(this.createWarning(lineNumber, col, col + collectionName.length, `Property '${collectionName}' is not a collection (array) type`));
                }

                if (!activeSchema.properties[joinProperty]) {
                    const col = line.indexOf(joinProperty, line.indexOf('on')) + 1;
                    markers.push(this.createWarning(lineNumber, col, col + joinProperty.length, `Property '${joinProperty}' not found in read model '${readModelName}'`));
                }
            }

            // Validate key references for composite keys
            const keyMatch = trimmed.match(/^key\s+(\w+)\s*$/);
            if (keyMatch && activeSchema) {
                const [, keyName] = keyMatch;
                // Check if it's a composite key type (should exist in definitions or as a property)
                const isInDefinitions = activeSchema.definitions && activeSchema.definitions[keyName];
                const isProperty = activeSchema.properties && activeSchema.properties[keyName];

                if (!isInDefinitions && !isProperty && !this.isBuiltInExpression(keyName)) {
                    const col = line.indexOf(keyName) + 1;
                    markers.push(this.createWarning(lineNumber, col, col + keyName.length, `Composite key type '${keyName}' not found in read model schema`));
                }
            }
        }

        // Validate read model has an identifier property, but only if there are from statements
        if (hasFromStatements && activeSchema?.properties) {
            const hasId = activeSchema.properties['id'] || activeSchema.properties['Id'];
            if (!hasId) {
                const col = firstLine.indexOf(readModelName) + 1;
                markers.push(this.createWarning(1, col, col + readModelName.length, `Read model '${readModelName}' should have an 'id' or 'Id' property to serve as identifier`));
            }
        }

        return markers;
    }

    private isBuiltInExpression(text: string): boolean {
        return text.startsWith('$') || ['true', 'false', 'null'].includes(text);
    }

    private createError(line: number, startCol: number, endCol: number, message: string): editor.IMarkerData {
        return {
            severity: 8, // Error
            startLineNumber: line,
            startColumn: startCol,
            endLineNumber: line,
            endColumn: endCol,
            message,
        };
    }

    private createWarning(line: number, startCol: number, endCol: number, message: string): editor.IMarkerData {
        return {
            severity: 4, // Warning
            startLineNumber: line,
            startColumn: startCol,
            endLineNumber: line,
            endColumn: endCol,
            message,
        };
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
}
