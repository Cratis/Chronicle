// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useRef, useState } from 'react';
import MonacoEditor, { Monaco, OnMount } from 'Components/MonacoEditor/MonacoEditor';
import type { editor, Uri } from 'monaco-editor';
import { registerCaptureDefinitionLanguage, languageId, disposeCaptureDefinitionLanguage } from './index';
import { Button } from 'primereact/button';
import { CaptureHelpPanel } from './CaptureHelpPanel';
import Strings from 'Strings';

export interface CaptureDeclarationSyntaxError {
    line: number;
    column: number;
    message: string;
}

export interface CaptureEditorProps {
    value: string;
    originalValue?: string;
    onChange?: (value: string) => void;
    onValidationChange?: (hasErrors: boolean) => void;
    errors?: CaptureDeclarationSyntaxError[];
    theme?: string;
}

export const CaptureEditor: React.FC<CaptureEditorProps> = ({
    value,
    originalValue,
    onChange,
    onValidationChange,
    errors,
    theme = 'vs-dark',
}) => {
    const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null);
    const monacoRef = useRef<Monaco | null>(null);
    const decorationsRef = useRef<string[]>([]);
    const [isHelpPanelOpen, setIsHelpPanelOpen] = useState(false);

    const handleEditorDidMount: OnMount = (editor, monacoInstance) => {
        editorRef.current = editor;
        monacoRef.current = monacoInstance;
        registerCaptureDefinitionLanguage(monacoInstance);

        const markerDisposable = monacoInstance.editor.onDidChangeMarkers((resources: readonly Uri[]) => {
            const model = editor.getModel();
            if (model && resources.some((resource: Uri) => resource.toString() === model.uri.toString())) {
                const markers = monacoInstance.editor.getModelMarkers({ resource: model.uri });
                const hasErrors = markers.some((marker: editor.IMarkerData) => marker.severity === monacoInstance.MarkerSeverity.Error);
                onValidationChange?.(hasErrors);
            }
        });

        const model = editor.getModel();
        if (model) {
            const markers = monacoInstance.editor.getModelMarkers({ resource: model.uri });
            const hasErrors = markers.some((marker: editor.IMarkerData) => marker.severity === monacoInstance.MarkerSeverity.Error);
            onValidationChange?.(hasErrors);
        }

        return () => {
            markerDisposable.dispose();
        };
    };

    useEffect(() => {
        return () => {
            disposeCaptureDefinitionLanguage();
        };
    }, []);

    useEffect(() => {
        if (!editorRef.current || !monacoRef.current) {
            return;
        }

        const model = editorRef.current.getModel();
        if (!model) {
            return;
        }

        if (errors && errors.length > 0) {
            const markers: editor.IMarkerData[] = errors.map(error => {
                const lineNumber = Math.min(Math.max(error.line, 1), model.getLineCount());
                const startColumn = Math.max(error.column, 1);
                const line = model.getLineContent(lineNumber);
                const endColumn = Math.max(startColumn + 1, line.length);
                return {
                    severity: monacoRef.current!.MarkerSeverity.Error,
                    startLineNumber: lineNumber,
                    startColumn,
                    endLineNumber: lineNumber,
                    endColumn,
                    message: error.message,
                };
            });
            monacoRef.current.editor.setModelMarkers(model, 'capture-declaration-server', markers);
        } else {
            monacoRef.current.editor.setModelMarkers(model, 'capture-declaration-server', []);
        }
    }, [errors]);

    useEffect(() => {
        if (!editorRef.current || !monacoRef.current) {
            return;
        }

        const editor = editorRef.current;
        const model = editor.getModel();
        if (!model) {
            return;
        }

        if (originalValue === undefined || originalValue === value) {
            decorationsRef.current = editor.deltaDecorations(decorationsRef.current, []);
            return;
        }

        const originalLines = originalValue.split('\n');
        const currentLines = value.split('\n');
        const decorations: editor.IModelDeltaDecoration[] = [];
        const maxLines = Math.max(originalLines.length, currentLines.length);

        for (let i = 0; i < maxLines; i++) {
            const originalLine = originalLines[i] ?? '';
            const currentLine = currentLines[i] ?? '';

            if (i >= currentLines.length) {
                continue;
            }

            if (i >= originalLines.length) {
                decorations.push({
                    range: { startLineNumber: i + 1, startColumn: 1, endLineNumber: i + 1, endColumn: 1 },
                    options: {
                        isWholeLine: true,
                        linesDecorationsClassName: 'modified-line-decoration added-line',
                        overviewRuler: {
                            color: '#587c0c',
                            position: monacoRef.current!.editor.OverviewRulerLane.Left,
                        },
                    },
                });
            } else if (originalLine !== currentLine) {
                decorations.push({
                    range: { startLineNumber: i + 1, startColumn: 1, endLineNumber: i + 1, endColumn: 1 },
                    options: {
                        isWholeLine: true,
                        linesDecorationsClassName: 'modified-line-decoration modified-line',
                        overviewRuler: {
                            color: '#0c7d9d',
                            position: monacoRef.current!.editor.OverviewRulerLane.Left,
                        },
                    },
                });
            }
        }

        decorationsRef.current = editor.deltaDecorations(decorationsRef.current, decorations);
    }, [value, originalValue]);

    return (
        <div style={{ position: 'relative', height: '100%', width: '100%', display: 'flex', overflow: 'hidden' }}>
            <div style={{ flex: 1, position: 'relative', height: '100%', minWidth: 0, overflow: 'hidden' }}>
                <Button
                    icon="pi pi-question-circle"
                    tooltip={Strings.components.captureEditor.tooltips.help}
                    tooltipOptions={{ position: 'left' }}
                    onClick={() => setIsHelpPanelOpen(!isHelpPanelOpen)}
                    className="p-button-rounded p-button-text p-button-lg"
                    style={{
                        position: 'absolute',
                        top: '8px',
                        right: '8px',
                        zIndex: 1000,
                    }}
                    pt={{
                        icon: { style: { fontSize: '1.5rem' } },
                        root: { style: { width: '3rem', height: '3rem' } },
                    }}
                />
                <MonacoEditor
                    height="100%"
                    language={languageId}
                    value={value}
                    theme={theme}
                    onChange={(newValue) => {
                        if (editorRef.current && monacoRef.current) {
                            const model = editorRef.current.getModel();
                            if (model) {
                                monacoRef.current.editor.setModelMarkers(model, 'capture-declaration-server', []);
                            }
                        }
                        onChange?.(newValue || '');
                    }}
                    onMount={handleEditorDidMount}
                    options={{
                        minimap: { enabled: false },
                        scrollBeyondLastLine: false,
                        fontSize: 14,
                        lineNumbers: 'on',
                        renderLineHighlight: 'all',
                        tabSize: 2,
                        hover: {
                            above: false,
                        },
                    }}
                />
            </div>

            <div
                style={{
                    width: isHelpPanelOpen ? '500px' : '0',
                    height: '100%',
                    overflow: 'hidden',
                    transition: 'width 0.3s ease-in-out',
                    borderLeft: isHelpPanelOpen ? '1px solid #3e3e42' : 'none',
                    display: isHelpPanelOpen ? 'flex' : 'none',
                    flexDirection: 'column',
                    backgroundColor: '#1e1e1e',
                }}
            >
                <div
                    style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        padding: '1rem',
                        borderBottom: '1px solid #3e3e42',
                        backgroundColor: '#252526',
                        minHeight: '60px',
                    }}
                >
                    <h3 style={{ margin: 0, color: '#ffffff', fontSize: '1.1rem' }}>{Strings.components.captureEditor.languageReference}</h3>
                    <Button
                        icon="pi pi-times"
                        onClick={() => setIsHelpPanelOpen(false)}
                        className="p-button-rounded p-button-text"
                        style={{ color: '#cccccc' }}
                    />
                </div>
                <div style={{ flex: 1, overflow: 'auto' }}>
                    <CaptureHelpPanel />
                </div>
            </div>
        </div>
    );
};
