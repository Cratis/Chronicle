// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor, languages, Position, IRange } from 'monaco-editor';

export class CaptureDefinitionLanguageHoverProvider implements languages.HoverProvider {
    provideHover(model: editor.ITextModel, position: Position): languages.ProviderResult<languages.Hover> {
        const token = this.getTokenAtPosition(model, position);
        if (!token) {
            return null;
        }

        const keywordInfo = this.getKeywordInfo(token.word);
        if (keywordInfo) {
            return {
                contents: [{ value: keywordInfo }],
                range: token.range,
            };
        }

        const expressionInfo = this.getExpressionInfo(token.word);
        if (expressionInfo) {
            return {
                contents: [{ value: expressionInfo }],
                range: token.range,
            };
        }

        return null;
    }

    private getTokenAtPosition(model: editor.ITextModel, position: Position): { word: string; range: IRange } | null {
        const lineContent = model.getLineContent(position.lineNumber);
        const tokens = [...lineContent.matchAll(/\$context|\$previous|\$env|[a-zA-Z_][\w-]*/g)];
        const column = position.column - 1;

        for (const token of tokens) {
            const startIndex = token.index ?? 0;
            const endIndex = startIndex + token[0].length;
            if (column >= startIndex && column <= endIndex) {
                return {
                    word: token[0],
                    range: {
                        startLineNumber: position.lineNumber,
                        endLineNumber: position.lineNumber,
                        startColumn: startIndex + 1,
                        endColumn: endIndex + 1,
                    },
                };
            }
        }

        return null;
    }

    private getKeywordInfo(word: string): string | null {
        const keywords: Record<string, string> = {
            capture: '**capture** *<Name>*\n\nDeclares a capture and gives it a unique name.',
            source: '**source** *<type>*\n\nDefines where the capture reads input from.',
            key: '**key** *<property>*\n\nSelects the property used to identify each captured item.',
            map: '**map**\n\nMaps source fields into a normalized shape for later append blocks.',
            append: '**append** *<EventName>*\n\nDeclares an event to append when the `when` clause matches.',
            when: '**when** *<condition>*\n\nSpecifies when an append block should fire.',
            nested: '**nested** *<property>*\n\nCreates a nested mapping scope for an object property.',
            children: '**children** *<collection>* **identified by** *<property>*\n\nCreates a scope for child collection items.',
            identified: '**identified**\n\nUsed with `children` to declare how child items are identified.',
            by: '**by**\n\nUsed in `children ... identified by`, `split ... by`, and similar clauses.',
            api: '**api**\n\nA source type for polling HTTP APIs.',
            webhook: '**webhook**\n\nA source type for receiving webhook payloads.',
            message: '**message**\n\nA source type for consuming messages from a topic.',
            url: '**url** *<address>*\n\nSpecifies the HTTP endpoint for an API source.',
            poll: '**poll** *<interval>*\n\nSets the polling interval for an API source.',
            auth: '**auth**\n\nConfigures authentication for the current source.',
            path: '**path** *<route>*\n\nConfigures the route for a webhook source.',
            topic: '**topic** *<name>*\n\nConfigures the topic for a message source.',
            from: '**from** *<value>*\n\nUsed in a `when` clause to match the previous value.',
            to: '**to** *<value>*\n\nUsed in a `when` clause to match the new value.',
            or: '**or**\n\nCombines multiple `when` conditions.',
            and: '**and**\n\nCombines multiple `when` conditions that must all match.',
            added: '**added**\n\nMatches when a child item was added.',
            removed: '**removed**\n\nMatches when a child item was removed.',
            translate: '**translate**\n\nMaps source values to new values inside a `map` block.',
            split: '**split** *<value>* **by** *<separator>*\n\nSplits a string into multiple mapped fields.',
            bearer: '**bearer**\n\nUses a bearer token for source authentication.',
        };

        return keywords[word] || null;
    }

    private getExpressionInfo(word: string): string | null {
        const expressions: Record<string, string> = {
            '$context': '**$context**\n\nCapture execution context.\n\n**Properties:**\n- `occurred` - When the current event should be considered to have occurred\n- `eventSourceId` - The resolved event source identifier',
            '$previous': '**$previous**\n\nThe previous state for the captured item. Use dot notation such as `$previous.status`.',
            '$env': '**$env**\n\nEnvironment variables. Use dot notation such as `$env.API_TOKEN`.',
        };

        return expressions[word] || null;
    }
}
