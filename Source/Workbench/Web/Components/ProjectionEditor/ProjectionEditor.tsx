// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useRef } from 'react';
import * as monaco from 'monaco-editor';
import { registerProjectionDslLanguage, setReadModelSchema, languageId, disposeProjectionDslLanguage } from './index';
import { JsonSchema } from 'Components/JsonSchema';

export interface ProjectionDslEditorProps {
    value: string;
    onChange?: (value: string) => void;
    readModelSchema?: JsonSchema,
    height?: string;
    theme?: string;
}

export const ProjectionDslEditor: React.FC<ProjectionDslEditorProps> = ({
    value,
    onChange,
    readModelSchema,
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
        if (readModelSchema) {
            setReadModelSchema(readModelSchema);
        }
    }, [readModelSchema]);

    // Update value when it changes externally
    useEffect(() => {
        if (editorRef.current && editorRef.current.getValue() !== value) {
            editorRef.current.setValue(value);
        }
    }, [value]);

    return <div ref={containerRef} style={{ height, width: '100%' }} />;
};
