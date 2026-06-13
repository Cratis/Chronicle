// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { languages, type editor, type Position, type IRange } from 'monaco-editor';

interface CompletionContext {
    textBeforeCursor: string;
    currentWord: string;
    indentLevel: number;
    lineNumber: number;
    wordStartColumn: number;
    wordEndColumn: number;
}

const commonPropertyPaths = [
    'id',
    'status',
    'firstName',
    'lastName',
    'fullName',
    'address.street',
    'address.city',
    'lineItems',
    'lineItems.quantity',
    'lineNumber',
    'quantity',
];

export class CaptureDefinitionLanguageCompletionProvider implements languages.CompletionItemProvider {
    provideCompletionItems(
        model: editor.ITextModel,
        position: Position
    ): languages.ProviderResult<languages.CompletionList> {
        const context = this.buildContext(model, position);
        const suggestions: languages.CompletionItem[] = [];

        if (context.lineNumber === 1) {
            this.addCaptureLineCompletions(suggestions, context);
        } else if (this.isAfterKeyword(context, 'capture')) {
            this.addCaptureNameCompletions(suggestions, context);
        } else if (this.isAfterKeyword(context, 'source')) {
            this.addSourceTypeCompletions(suggestions, context);
        } else if (this.isAfterAppendKeyword(context)) {
            this.addAppendNameCompletions(suggestions, context);
        } else if (this.isAfterWhenKeyword(context)) {
            this.addWhenCompletions(suggestions, context);
        } else if (this.isAfterWhenFrom(context)) {
            this.addFromValueCompletions(suggestions, context);
        } else if (this.isAfterWhenTo(context)) {
            this.addToValueCompletions(suggestions, context);
        } else if (this.isAfterContextPrefix(context, '$.')) {
            this.addPathCompletions(suggestions, context, '$.');
        } else if (this.isAfterContextPrefix(context, '$previous.')) {
            this.addPathCompletions(suggestions, context, '$previous.');
        } else if (this.isAfterContextPrefix(context, '$context.')) {
            this.addContextCompletions(suggestions, context);
        } else if (this.isAfterContextPrefix(context, '$env.')) {
            this.addEnvironmentCompletions(suggestions, context);
        } else if (this.isAfterAssignment(context)) {
            this.addAssignmentValueCompletions(suggestions, context);
        } else if ((this.isStartOfLine(context) || this.isAfterIndent(context)) && context.indentLevel === 1) {
            this.addTopLevelStatementCompletions(suggestions, context);
        } else if ((this.isStartOfLine(context) || this.isAfterIndent(context)) && context.indentLevel >= 2) {
            this.addSourceBlockCompletions(suggestions, context, this.getCurrentSourceType(model, position.lineNumber));
        }

        return { suggestions };
    }

    private buildContext(model: editor.ITextModel, position: Position): CompletionContext {
        const lineContent = model.getLineContent(position.lineNumber);
        const textBeforeCursor = lineContent.substring(0, position.column - 1);
        const word = model.getWordUntilPosition(position);

        return {
            textBeforeCursor,
            currentWord: word.word || '',
            indentLevel: this.getIndentLevel(textBeforeCursor),
            lineNumber: position.lineNumber,
            wordStartColumn: word.startColumn,
            wordEndColumn: word.endColumn,
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

    private isAfterAppendKeyword(context: CompletionContext): boolean {
        return /\bappend\s+[\w.]*$/.test(context.textBeforeCursor);
    }

    private isAfterWhenKeyword(context: CompletionContext): boolean {
        return /\bwhen\s+[\w.]*$/.test(context.textBeforeCursor);
    }

    private isAfterWhenFrom(context: CompletionContext): boolean {
        return /\bwhen\s+[\w.]+\s+from\s+[^\s]*$/.test(context.textBeforeCursor);
    }

    private isAfterWhenTo(context: CompletionContext): boolean {
        return /\bwhen\s+[\w.]+\s+from\s+\S+\s+to\s+[^\s]*$/.test(context.textBeforeCursor);
    }

    private isAfterAssignment(context: CompletionContext): boolean {
        return /=\s*([$\w.]*)$/.test(context.textBeforeCursor);
    }

    private isAfterContextPrefix(context: CompletionContext, prefix: string): boolean {
        return context.textBeforeCursor.endsWith(prefix);
    }

    private addCaptureLineCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        const trimmed = context.textBeforeCursor.trim();
        if (trimmed === '' || 'capture'.startsWith(trimmed)) {
            suggestions.push({
                label: 'capture',
                kind: languages.CompletionItemKind.Keyword,
                insertText: 'capture ',
                documentation: 'Define a capture declaration',
                detail: 'capture <Name>',
                range: this.getRangeForWord(context),
            });
        }

        if (trimmed.startsWith('capture ')) {
            this.addCaptureNameCompletions(suggestions, context);
        }
    }

    private addCaptureNameCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        suggestions.push({
            label: 'CaptureDefinition',
            kind: languages.CompletionItemKind.Class,
            insertText: 'CaptureDefinition',
            documentation: 'A descriptive name for this capture declaration',
            detail: 'Capture name',
            range: this.getRangeForWord(context),
        });
    }

    private addSourceTypeCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        [
            { label: 'api', detail: 'source api', documentation: 'Capture data by polling an HTTP API' },
            { label: 'webhook', detail: 'source webhook', documentation: 'Capture data from incoming webhook requests' },
            { label: 'message', detail: 'source message', documentation: 'Capture data from a message topic' },
        ].forEach(source => {
            suggestions.push({
                label: source.label,
                kind: languages.CompletionItemKind.Keyword,
                insertText: source.label,
                documentation: source.documentation,
                detail: source.detail,
                range: this.getRangeForWord(context),
            });
        });
    }

    private addSourceBlockCompletions(suggestions: languages.CompletionItem[], context: CompletionContext, sourceType: string | null): void {
        const options: Record<string, Array<{ label: string; detail: string; documentation: string }>> = {
            api: [
                { label: 'url', detail: 'url <https://...>', documentation: 'The URL to poll for data' },
                { label: 'poll', detail: 'poll <interval>', documentation: 'The polling interval, for example 5m' },
                { label: 'auth', detail: 'auth bearer $env.API_TOKEN', documentation: 'Authentication for the API call' },
            ],
            webhook: [
                { label: 'path', detail: 'path <route>', documentation: 'The relative webhook path to receive payloads on' },
                { label: 'auth', detail: 'auth bearer $env.WEBHOOK_TOKEN', documentation: 'Authentication for the webhook call' },
            ],
            message: [
                { label: 'topic', detail: 'topic <name>', documentation: 'The topic or subject to subscribe to' },
            ],
        };

        (sourceType ? options[sourceType] ?? [] : []).forEach(option => {
            suggestions.push({
                label: option.label,
                kind: languages.CompletionItemKind.Keyword,
                insertText: `${option.label} `,
                documentation: option.documentation,
                detail: option.detail,
                range: this.getRangeForWord(context),
            });
        });
    }

    private addTopLevelStatementCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        [
            { label: 'key', insertText: 'key ', detail: 'key <property>', documentation: 'Identify the captured entity' },
            { label: 'map', insertText: 'map', detail: 'map', documentation: 'Map captured fields into a normalized shape' },
            { label: 'append', insertText: 'append ', detail: 'append <EventName>', documentation: 'Append an event when a condition matches' },
            { label: 'nested', insertText: 'nested ', detail: 'nested <property>', documentation: 'Create a nested mapping scope' },
            { label: 'children', insertText: 'children ', detail: 'children <collection> identified by <property>', documentation: 'Handle child collections independently' },
        ].forEach(statement => {
            suggestions.push({
                label: statement.label,
                kind: languages.CompletionItemKind.Keyword,
                insertText: statement.insertText,
                documentation: statement.documentation,
                detail: statement.detail,
                range: this.getRangeForWord(context),
            });
        });
    }

    private addAppendNameCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        suggestions.push({
            label: 'ItemChanged',
            kind: languages.CompletionItemKind.Class,
            insertText: 'ItemChanged',
            documentation: 'A generic event name for the appended event',
            detail: 'Event name',
            range: this.getRangeForWord(context),
        });
    }

    private addWhenCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        [
            { label: 'added', insertText: 'added', detail: 'when added', documentation: 'Append when a child item was added' },
            { label: 'removed', insertText: 'removed', detail: 'when removed', documentation: 'Append when a child item was removed' },
            { label: 'status', insertText: 'status', detail: 'when <property>', documentation: 'Append when a property changed' },
            { label: 'status transition', insertText: 'status from inactive to active', detail: 'when <property> from <value> to <value>', documentation: 'Append when a property changed between two values' },
            { label: 'quantity', insertText: 'quantity', detail: 'when <property>', documentation: 'Append when a property changed' },
            { label: 'lineNumber', insertText: 'lineNumber', detail: 'when <property>', documentation: 'Append when a specific property changed' },
        ].forEach(option => {
            suggestions.push({
                label: option.label,
                kind: languages.CompletionItemKind.Keyword,
                insertText: option.insertText,
                documentation: option.documentation,
                detail: option.detail,
                range: this.getRangeForWord(context),
            });
        });
    }

    private addFromValueCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        [
            { label: '*', insertText: '*', detail: 'Wildcard', documentation: 'Match any previous value' },
            { label: 'inactive', insertText: 'inactive', detail: 'Common value', documentation: 'A common previous state value' },
            { label: 'active', insertText: 'active', detail: 'Common value', documentation: 'A common previous state value' },
            { label: 'true', insertText: 'true', detail: 'Boolean value', documentation: 'Boolean true' },
            { label: 'false', insertText: 'false', detail: 'Boolean value', documentation: 'Boolean false' },
        ].forEach(option => {
            suggestions.push({
                label: option.label,
                kind: languages.CompletionItemKind.Value,
                insertText: option.insertText,
                documentation: option.documentation,
                detail: option.detail,
                range: this.getRangeForWord(context),
            });
        });
    }

    private addToValueCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        ['active', 'inactive', 'pending', 'true', 'false'].forEach(value => {
            suggestions.push({
                label: value,
                kind: languages.CompletionItemKind.Value,
                insertText: value,
                documentation: `Common target value: ${value}`,
                detail: 'Target value',
                range: this.getRangeForWord(context),
            });
        });
    }

    private addAssignmentValueCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        [
            { label: '$.', insertText: '$.', detail: 'Current captured value', documentation: 'Reference the current captured payload' },
            { label: '$previous.', insertText: '$previous.', detail: 'Previous captured value', documentation: 'Reference the previous state of the payload' },
            { label: '$context.occurred', insertText: '$context.occurred', detail: 'Occurred timestamp', documentation: 'When the capture source produced the current data' },
            { label: '$context.eventSourceId', insertText: '$context.eventSourceId', detail: 'Event source identifier', documentation: 'The resolved event source identifier for the append operation' },
            { label: '$env.', insertText: '$env.', detail: 'Environment variable', documentation: 'Read an environment variable' },
        ].forEach(option => {
            suggestions.push({
                label: option.label,
                kind: languages.CompletionItemKind.Variable,
                insertText: option.insertText,
                documentation: option.documentation,
                detail: option.detail,
                range: this.getRangeForWord(context),
            });
        });
    }

    private addPathCompletions(suggestions: languages.CompletionItem[], context: CompletionContext, prefix: string): void {
        commonPropertyPaths.forEach(path => {
            suggestions.push({
                label: path,
                kind: languages.CompletionItemKind.Property,
                insertText: `${prefix}${path}`,
                documentation: `Common property path: ${prefix}${path}`,
                detail: prefix === '$.' ? 'Current value path' : 'Previous value path',
                range: this.getRangeForWord(context),
            });
        });
    }

    private addContextCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        [
            { label: 'occurred', insertText: '$context.occurred', detail: 'When the event occurred', documentation: 'Timestamp for when the append event happened' },
            { label: 'eventSourceId', insertText: '$context.eventSourceId', detail: 'Event source identifier', documentation: 'The event source identifier used for the appended event' },
        ].forEach(option => {
            suggestions.push({
                label: option.label,
                kind: languages.CompletionItemKind.Property,
                insertText: option.insertText,
                documentation: option.documentation,
                detail: option.detail,
                range: this.getRangeForWord(context),
            });
        });
    }

    private addEnvironmentCompletions(suggestions: languages.CompletionItem[], context: CompletionContext): void {
        suggestions.push({
            label: 'API_TOKEN',
            kind: languages.CompletionItemKind.Variable,
            insertText: '$env.API_TOKEN',
            documentation: 'Placeholder environment variable reference',
            detail: 'Environment variable',
            range: this.getRangeForWord(context),
        });
    }

    private getCurrentSourceType(model: editor.ITextModel, lineNumber: number): string | null {
        for (let i = lineNumber; i >= 1; i--) {
            const line = model.getLineContent(i).trim();
            const match = line.match(/^source\s+(api|webhook|message)\b/);
            if (match) {
                return match[1];
            }
        }
        return null;
    }

    private getRangeForWord(context: CompletionContext): IRange {
        return {
            startLineNumber: context.lineNumber,
            endLineNumber: context.lineNumber,
            startColumn: context.wordStartColumn,
            endColumn: context.wordEndColumn,
        };
    }
}
