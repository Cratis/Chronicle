// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useRef } from 'react';
import * as monaco from 'monaco-editor';
import {
    registerProjectionDslLanguage,
    setReadModelSchemas,
    setEventSchemas,
    languageId,
    disposeProjectionDslLanguage,
} from './index';
import { JsonSchema } from 'Components/JsonSchema';
import { ProjectionDefinitionSyntaxError } from 'Api/Projections';

export interface ProjectionEditorProps {
    value: string;
    onChange?: (value: string) => void;
    readModelSchemas?: JsonSchema[],
    eventSchemas?: JsonSchema[],
    errors?: ProjectionDefinitionSyntaxError[];
    height?: string;
    theme?: string;
}

export const ProjectionEditor: React.FC<ProjectionEditorProps> = ({
    value,
    onChange,
    readModelSchemas,
    eventSchemas,
    errors,
    height = '400px',
    theme = 'vs-dark',
}) => {
    const editorRef = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);
    const containerRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (!containerRef.current) return;

        // Register the language (only once)
        registerProjectionDslLanguage(monaco);

        // Create editor
        editorRef.current = monaco.editor.create(containerRef.current, {
            value,
            language: languageId,
            theme,
            minimap: { enabled: false },
            automaticLayout: true,
            scrollBeyondLastLine: false,
            fontSize: 14,
            lineNumbers: 'on',
            renderLineHighlight: 'all',
            tabSize: 2,
        });

        // Listen to content changes
        if (onChange) {
            editorRef.current.onDidChangeModelContent(() => {
                const newValue = editorRef.current?.getValue() || '';
                onChange(newValue);
            });
        }

        return () => {
            editorRef.current?.dispose();
            disposeProjectionDslLanguage();
        };
    }, []);

    // Update schema when it changes
    useEffect(() => {
        if (readModelSchemas) {
            setReadModelSchemas(readModelSchemas as any);
        }
        if (eventSchemas) {
            setEventSchemas(eventSchemas as any);
        }
    }, [readModelSchemas, eventSchemas]);

    // Update value when it changes externally
    useEffect(() => {
        if (editorRef.current && editorRef.current.getValue() !== value) {
            editorRef.current.setValue(value);
        }
    }, [value]);

    // Update markers when errors change
    useEffect(() => {
        if (!editorRef.current) return;

        const model = editorRef.current.getModel();
        if (!model) return;

        if (errors && errors.length > 0) {
            console.log('Setting markers for errors:', errors);
            const markers: monaco.editor.IMarkerData[] = errors.map(error => {
                const line = model.getLineContent(error.line);
                const endColumn = Math.max(error.column + 1, line.length);
                return {
                    severity: monaco.MarkerSeverity.Error,
                    startLineNumber: error.line,
                    startColumn: error.column,
                    endLineNumber: error.line,
                    endColumn: endColumn,
                    message: error.message,
                };
            });
            console.log('Markers:', markers);
            monaco.editor.setModelMarkers(model, 'projection-dsl', markers);
        } else {
            console.log('Clearing markers');
            monaco.editor.setModelMarkers(model, 'projection-dsl', []);
        }
    }, [errors]);

    return <div ref={containerRef} style={{ height, width: '100%' }} />;
};
