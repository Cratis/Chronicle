// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { MarkerSeverity, type editor } from 'monaco-editor';

export class CaptureDefinitionLanguageValidator {
    validate(model: editor.ITextModel): editor.IMarkerData[] {
        const markers: editor.IMarkerData[] = [];
        const content = model.getValue();

        if (!content.trim()) {
            return markers;
        }

        const lines = content.replace(/\r\n/g, '\n').replace(/\r/g, '\n').split('\n');

        let firstNonEmptyLineIndex = -1;
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
            return markers;
        }

        const captureMatch = firstLine.match(/^capture\s+([\w.]+)\s*$/);
        if (!captureMatch) {
            const lineNumber = firstNonEmptyLineIndex + 1;
            markers.push(this.createError(lineNumber, 1, firstLine.length + 1, 'Capture definition must start with "capture <Name>"'));
            return markers;
        }

        const meaningfulLines = lines
            .map((line, index) => ({
                line,
                trimmed: line.trim(),
                lineNumber: index + 1,
                indent: line.search(/\S/),
            }))
            .filter(line => line.trimmed && !line.trimmed.startsWith('#'));

        const hasSource = meaningfulLines.some(line => line.trimmed.startsWith('source '));
        if (!hasSource) {
            markers.push(this.createError(firstNonEmptyLineIndex + 1, 1, firstLine.length + 1, 'Capture definition must include a "source" block'));
        }

        const hasKey = meaningfulLines.some(line => line.trimmed.startsWith('key '));
        if (!hasKey) {
            markers.push(this.createError(firstNonEmptyLineIndex + 1, 1, firstLine.length + 1, 'Capture definition must include a "key" declaration'));
        }

        for (let i = 0; i < meaningfulLines.length; i++) {
            const current = meaningfulLines[i];
            if (!current.trimmed.startsWith('append ')) {
                continue;
            }

            let hasWhenClause = false;
            for (let j = i + 1; j < meaningfulLines.length; j++) {
                const next = meaningfulLines[j];
                if (next.indent <= current.indent) {
                    break;
                }
                if (next.trimmed.startsWith('when ')) {
                    hasWhenClause = true;
                    break;
                }
            }

            if (!hasWhenClause) {
                const eventName = current.trimmed.substring('append '.length).trim() || 'unnamed append block';
                const startColumn = current.line.indexOf(eventName) + 1;
                markers.push(this.createError(
                    current.lineNumber,
                    startColumn > 0 ? startColumn : 1,
                    startColumn > 0 ? startColumn + eventName.length : current.line.length + 1,
                    `Append block '${eventName}' must include a "when" clause`));
            }
        }

        return markers;
    }

    createError(line: number, startCol: number, endCol: number, message: string): editor.IMarkerData {
        return {
            severity: MarkerSeverity.Error,
            startLineNumber: line,
            startColumn: startCol,
            endLineNumber: line,
            endColumn: endCol,
            message,
        };
    }

}
