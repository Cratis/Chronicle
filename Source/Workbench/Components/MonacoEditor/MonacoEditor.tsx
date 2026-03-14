// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useRef, useEffect } from 'react';
import * as monaco from 'monaco-editor';
import EditorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker';

self.MonacoEnvironment = {
    getWorker(): Worker {
        return new EditorWorker();
    },
};

export type Monaco = typeof monaco;

export type OnMount = (
    editor: monaco.editor.IStandaloneCodeEditor,
    monacoInstance: Monaco,
) => void | (() => void);

export interface MonacoEditorProps {
    height?: string | number;
    language?: string;
    value?: string;
    theme?: string;
    onChange?: (value: string | undefined) => void;
    onMount?: OnMount;
    options?: monaco.editor.IStandaloneEditorConstructionOptions;
}

export const MonacoEditor: React.FC<MonacoEditorProps> = ({
    height = '100%',
    language,
    value,
    theme,
    onChange,
    onMount,
    options,
}) => {
    const containerRef = useRef<HTMLDivElement>(null);
    const editorRef = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);
    const onChangeRef = useRef(onChange);
    onChangeRef.current = onChange;

    useEffect(() => {
        if (!containerRef.current) return;

        const editor = monaco.editor.create(containerRef.current, {
            value: value ?? '',
            language,
            theme,
            automaticLayout: true,
            ...options,
        });
        editorRef.current = editor;

        const changeDisposable = editor.onDidChangeModelContent(() => {
            onChangeRef.current?.(editor.getValue());
        });

        let unmountCleanup: (() => void) | void;
        if (onMount) {
            unmountCleanup = onMount(editor, monaco);
        }

        return () => {
            changeDisposable.dispose();
            if (typeof unmountCleanup === 'function') {
                unmountCleanup();
            }
            editor.dispose();
            editorRef.current = null;
        };
    }, []);

    useEffect(() => {
        if (!editorRef.current) return;
        if (editorRef.current.getValue() !== (value ?? '')) {
            editorRef.current.setValue(value ?? '');
        }
    }, [value]);

    useEffect(() => {
        if (!editorRef.current || !language) return;
        const model = editorRef.current.getModel();
        if (model) {
            monaco.editor.setModelLanguage(model, language);
        }
    }, [language]);

    useEffect(() => {
        if (theme) {
            monaco.editor.setTheme(theme);
        }
    }, [theme]);

    useEffect(() => {
        if (!editorRef.current || !options) return;
        editorRef.current.updateOptions(options);
    }, [options]);

    return <div ref={containerRef} style={{ height, width: '100%' }} />;
};

export default MonacoEditor;
