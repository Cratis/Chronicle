// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor, languages, Position } from 'monaco-editor';
import type { JsonSchema, JsonSchemaProperty } from '../JsonSchema';
import type { ReadModelInfo } from './index';

export class ProjectionDslHoverProvider implements languages.HoverProvider {
    private readModelSchemas: JsonSchema[] = [];
    private eventSchemas: Record<string, JsonSchema> = {};


    setReadModels(readModels: ReadModelInfo[]): void {
        this.readModelSchemas = (readModels || []).map(rm => rm.schema);
    }

    // Keep for backwards compatibility
    setReadModelSchemas(schemas: JsonSchema[]): void {
        this.readModelSchemas = schemas;
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

    provideHover(model: editor.ITextModel, position: Position): languages.ProviderResult<languages.Hover> {
        const word = model.getWordAtPosition(position);
        if (!word) return null;

        const wordText = word.word;

        // Check if it's a keyword
        const keywordInfo = this.getKeywordInfo(wordText);
        if (keywordInfo) {
            return {
                contents: [{ value: keywordInfo }],
                range: {
                    startLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endLineNumber: position.lineNumber,
                    endColumn: word.endColumn,
                },
            };
        }

        // Check if it's a built-in expression
        const expressionInfo = this.getExpressionInfo(wordText);
        if (expressionInfo) {
            return {
                contents: [{ value: expressionInfo }],
                range: {
                    startLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endLineNumber: position.lineNumber,
                    endColumn: word.endColumn,
                },
            };
        }

        // Check if it's an event type
        if (this.eventSchemas[wordText]) {
            const schema = this.eventSchemas[wordText];
            const description = this.getSchemaDescription(schema);
            const properties = this.getSchemaProperties(schema);

            let content = `**Event Type:** \`${wordText}\`\n\n`;
            if (description) {
                content += `${description}\n\n`;
            }
            if (properties.length > 0) {
                content += `**Properties:**\n\n${properties.map(p => `- \`${p.name}\`: ${p.type}`).join('\n')}`;
            }

            return {
                contents: [{ value: content }],
                range: {
                    startLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endLineNumber: position.lineNumber,
                    endColumn: word.endColumn,
                },
            };
        }

        // Check if it's a read model
        const readModel = this.readModelSchemas.find(s => this.getSchemaName(s) === wordText);
        if (readModel) {
            const description = this.getSchemaDescription(readModel);
            const properties = this.getSchemaProperties(readModel);

            let content = `**Read Model:** \`${wordText}\`\n\n`;
            if (description) {
                content += `${description}\n\n`;
            }
            if (properties.length > 0) {
                content += `**Properties:**\n\n${properties.map(p => `- \`${p.name}\`: ${p.type}`).join('\n')}`;
            }

            return {
                contents: [{ value: content }],
                range: {
                    startLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endLineNumber: position.lineNumber,
                    endColumn: word.endColumn,
                },
            };
        }

        // Check if it's a property in the current context
        const propertyInfo = this.getPropertyInfo(model, position, wordText);
        if (propertyInfo) {
            return {
                contents: [{ value: propertyInfo }],
                range: {
                    startLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endLineNumber: position.lineNumber,
                    endColumn: word.endColumn,
                },
            };
        }

        return null;
    }

    private getKeywordInfo(word: string): string | null {
        const keywords: Record<string, string> = {
            'projection': '**projection** *<ProjectionName>* **=>** *<ReadModelName>*\n\nDefines a projection that transforms events into a read model.',
            'from': '**from** *<EventType>*\n\nHandles specific event types and maps their properties to the read model.',
            'every': '**every**\n\nHandles all events with automatic property mapping.',
            'key': '**key** *<expression>*\n\nDefines the projection key or composite key type.',
            'parent': '**parent** *<expression>*\n\nDefines a parent relationship for hierarchical projections.',
            'join': '**join** *<collection>* **on** *<property>*\n\nJoins with a child collection based on a property.',
            'children': '**children** *<collection>* **id** *<expression>*\n\nDefines operations on child collections.',
            'remove': '**remove with** *<EventType>* or **remove via join on** *<EventType>*\n\nRemoves items from the read model based on events.',
            'automap': '**automap** or **no automap**\n\nControls automatic property mapping from events to the read model.',
            'increment': '**increment** *<property>*\n\nIncrements a numeric property by 1.',
            'decrement': '**decrement** *<property>*\n\nDecrements a numeric property by 1.',
            'count': '**count** *<property>*\n\nCounts occurrences and stores in a property.',
            'add': '**add** *<property>* **by** *<expression>*\n\nAdds a value to a property.',
            'subtract': '**subtract** *<property>* **by** *<expression>*\n\nSubtracts a value from a property.',
            'exclude': '**exclude children**\n\nExcludes child collections from automatic mapping.',
            'events': '**events** *<EventType>*, ...\n\nSpecifies which event types to include in a join.',
            'via': 'Used with **remove via join on** to remove items via a join relationship.',
            'with': 'Used with **remove with** to specify which event type triggers removal.',
            'on': 'Used with **join** to specify the join property.',
            'by': 'Used with **add** or **subtract** to specify the value.',
            'id': 'Used with **children** to specify the child identifier expression.',
            'no': 'Used with **no automap** to disable automatic property mapping.',
        };

        return keywords[word] || null;
    }

    private getExpressionInfo(word: string): string | null {
        const expressions: Record<string, string> = {
            '$eventSourceId': '**$eventSourceId**\n\nThe identifier of the event source (aggregate/entity) that generated the event.',
            '$causedBy': '**$causedBy**\n\nInformation about who/what caused the event.\n\n**Properties:**\n- `subject` - The subject identifier\n- `name` - The display name\n- `userName` - The username',
            '$eventContext': '**$eventContext**\n\nAccess to event context properties.\n\n**Properties:**\n- `sequenceNumber` - The event sequence number\n- `occurred` - When the event occurred\n- `eventSourceId` - The event source identifier\n- `namespace` - The event namespace',
            '$occurred': '**$occurred**\n\nThe timestamp when the event occurred.',
            '$namespace': '**$namespace**\n\nThe namespace of the event.',
        };

        return expressions[word] || null;
    }

    private getPropertyInfo(model: editor.ITextModel, position: Position, propertyName: string): string | null {
        // Try to find the active read model
        const firstLine = model.getLineContent(1).trim();
        const match = firstLine.match(/^projection\s+\w+\s*=>\s*(\w+)/);
        if (!match) return null;

        const readModelName = match[1];
        const readModel = this.readModelSchemas.find(s => this.getSchemaName(s) === readModelName);

        if (readModel?.properties && readModel.properties[propertyName]) {
            const prop = readModel.properties[propertyName];
            const type = this.getPropertyTypeDescription(prop);
            const description = prop.description;

            let content = `**Property:** \`${propertyName}\`\n\n**Type:** \`${type}\``;
            if (description) {
                content += `\n\n${description}`;
            }
            return content;
        }

        // Check if it's an event property
        const eventType = this.getCurrentEventType(model, position.lineNumber);
        if (eventType && this.eventSchemas[eventType]) {
            const eventSchema = this.eventSchemas[eventType];
            if (eventSchema.properties && eventSchema.properties[propertyName]) {
                const prop = eventSchema.properties[propertyName];
                const type = this.getPropertyTypeDescription(prop);
                const description = prop.description;

                let content = `**Event Property:** \`${propertyName}\`\n\n**Type:** \`${type}\``;
                if (description) {
                    content += `\n\n${description}`;
                }
                return content;
            }
        }

        return null;
    }

    private getCurrentEventType(model: editor.ITextModel, lineNumber: number): string | null {
        for (let i = lineNumber; i >= 1; i--) {
            const line = model.getLineContent(i).trim();
            const fromMatch = line.match(/^from\s+(\w+)/);
            if (fromMatch) {
                return fromMatch[1];
            }
        }
        return null;
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

    private getSchemaProperties(schema: JsonSchema): Array<{ name: string; type: string }> {
        if (!schema.properties) return [];

        return Object.entries(schema.properties).map(([name, prop]) => ({
            name,
            type: this.getPropertyTypeDescription(prop),
        }));
    }

    private getPropertyTypeDescription(propSchema: JsonSchemaProperty): string {
        if (propSchema.type === 'array' && propSchema.items) {
            const itemType = propSchema.items.type || propSchema.items.$ref?.split('/').pop() || 'unknown';
            return `${itemType}[]`;
        }
        return propSchema.type || propSchema.$ref?.split('/').pop() || 'unknown';
    }
}
