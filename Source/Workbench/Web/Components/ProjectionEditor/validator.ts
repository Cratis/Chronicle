// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor } from 'monaco-editor';
import type { JsonSchema } from '../JsonSchema';

export class ProjectionDslValidator {
    private readModelSchemas: JsonSchema[] = [];
    private eventSchemas: Record<string, JsonSchema> = {};

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
        const lines = content.split('\n');

        if (lines.length === 0 || !lines[0].trim()) {
            markers.push(this.createError(1, 1, 1, 'Projection definition must start with "projection <ProjectionName> => <ReadModelName>"'));
            return markers;
        }

        // Validate first line - projection declaration
        const firstLine = lines[0].trim();
        const projectionMatch = firstLine.match(/^projection\s+(\w+)\s*=>\s*(\w+)\s*$/);

        if (!projectionMatch) {
            markers.push(this.createError(1, 1, firstLine.length + 1, 'Projection definition must start with "projection <ProjectionName> => <ReadModelName>"'));
            return markers;
        }

        const [, projectionName, readModelName] = projectionMatch;

        // Validate read model exists in schemas
        if (this.readModelSchemas.length > 0) {
            const readModelExists = this.readModelSchemas.some((s) => this.getSchemaName(s) === readModelName);
            if (!readModelExists) {
                const col = firstLine.indexOf(readModelName) + 1;
                markers.push(this.createWarning(1, col, col + readModelName.length, `Read model '${readModelName}' not found in available schemas`));
            }
        }

        // Get the active read model schema for property validation
        const activeSchema = this.readModelSchemas.find((s) => this.getSchemaName(s) === readModelName);

        // Validate subsequent lines
        for (let i = 1; i < lines.length; i++) {
            const lineNumber = i + 1;
            const line = lines[i];
            const trimmed = line.trim();

            if (!trimmed || trimmed.startsWith('#')) continue;

            // Validate event types in from/every blocks
            const fromMatch = trimmed.match(/^from\s+(\w+)/);
            if (fromMatch) {
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
            if (assignmentMatch && activeSchema?.properties) {
                const [, propertyName] = assignmentMatch;
                if (!activeSchema.properties[propertyName]) {
                    const col = line.indexOf(propertyName) + 1;
                    markers.push(this.createWarning(lineNumber, col, col + propertyName.length, `Property '${propertyName}' not found in read model '${readModelName}'`));
                }
            }

            // Validate increment/decrement/count targets
            const numericOpMatch = trimmed.match(/^(?:increment|decrement|count)\s+(\w+)$/);
            if (numericOpMatch && activeSchema?.properties) {
                const [, propertyName] = numericOpMatch;
                if (!activeSchema.properties[propertyName]) {
                    const col = line.indexOf(propertyName) + 1;
                    markers.push(this.createWarning(lineNumber, col, col + propertyName.length, `Property '${propertyName}' not found in read model '${readModelName}'`));
                }
            }

            // Validate add/subtract targets
            const addSubMatch = trimmed.match(/^(?:add|subtract)\s+(\w+)\s+by\s+/);
            if (addSubMatch && activeSchema?.properties) {
                const [, propertyName] = addSubMatch;
                if (!activeSchema.properties[propertyName]) {
                    const col = line.indexOf(propertyName) + 1;
                    markers.push(this.createWarning(lineNumber, col, col + propertyName.length, `Property '${propertyName}' not found in read model '${readModelName}'`));
                }
            }

            // Validate children references
            const childrenMatch = trimmed.match(/^children\s+(\w+)\s+id\s+/);
            if (childrenMatch && activeSchema?.properties) {
                const [, propertyName] = childrenMatch;
                const prop = activeSchema.properties[propertyName];
                if (!prop) {
                    const col = line.indexOf(propertyName) + 1;
                    markers.push(this.createWarning(lineNumber, col, col + propertyName.length, `Collection property '${propertyName}' not found in read model '${readModelName}'`));
                } else if (prop.type !== 'array' && !prop.items) {
                    const col = line.indexOf(propertyName) + 1;
                    markers.push(this.createWarning(lineNumber, col, col + propertyName.length, `Property '${propertyName}' is not a collection (array) type`));
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
        if ((schema as any).name) return (schema as any).name;
        if ((schema as any).title) return (schema as any).title;
        if (typeof (schema as any).$id === 'string') {
            const parts = (schema as any).$id.split('/');
            return parts[parts.length - 1] || (schema as any).$id;
        }
        return undefined;
    }
}
