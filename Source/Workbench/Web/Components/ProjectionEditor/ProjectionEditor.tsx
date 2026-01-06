// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useRef, useState } from 'react';
import * as monaco from 'monaco-editor';
import {
    registerProjectionDslLanguage,
    setReadModelSchemas,
    setEventSchemas,
    languageId,
    disposeProjectionDslLanguage,
} from './index';
import { JsonSchema } from 'Components/JsonSchema';
import { ProjectionDefinitionSyntaxError, GenerateDeclarativeCode, GenerateModelBoundCode } from 'Api/Projections';
import { Button } from 'primereact/button';
import { ProjectionHelpPanel } from './ProjectionHelpPanel';
import { ProjectionCodePanel } from './ProjectionCodePanel';
import Strings from 'Strings';

export interface ProjectionEditorProps {
    value: string;
    onChange?: (value: string) => void;
    readModelSchemas?: JsonSchema[],
    eventSchemas?: JsonSchema[],
    errors?: ProjectionDefinitionSyntaxError[];
    height?: string;
    theme?: string;
    eventStore?: string;
    namespace?: string;
}

export const ProjectionEditor: React.FC<ProjectionEditorProps> = ({
    value,
    onChange,
    readModelSchemas,
    eventSchemas,
    errors,
    height = '400px',
    theme = 'vs-dark',
    eventStore,
    namespace,
}) => {
    const editorRef = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);
    const containerRef = useRef<HTMLDivElement>(null);
    const [isHelpPanelOpen, setIsHelpPanelOpen] = useState(false);
    const [isCodePanelOpen, setIsCodePanelOpen] = useState(false);
    const [declarativeCode, setDeclarativeCode] = useState('');
    const [modelBoundCode, setModelBoundCode] = useState('');
    const [generateDeclarativeCode] = GenerateDeclarativeCode.use();
    const [generateModelBoundCode] = GenerateModelBoundCode.use();

    const fetchCode = async () => {
        if (eventStore && namespace && value) {
            generateDeclarativeCode.eventStore = eventStore;
            generateDeclarativeCode.namespace = namespace;
            generateDeclarativeCode.dsl = value;
            const declarativeResult = await generateDeclarativeCode.execute();

            generateModelBoundCode.eventStore = eventStore;
            generateModelBoundCode.namespace = namespace;
            generateModelBoundCode.dsl = value;
            const modelBoundResult = await generateModelBoundCode.execute();

            const declCode = declarativeResult.response?.code || declarativeResult.code || '// Unable to generate code';
            const modelCode = modelBoundResult.response?.code || modelBoundResult.code || '// Unable to generate code';

            setDeclarativeCode(declCode);
            setModelBoundCode(modelCode);
        }
    };

    useEffect(() => {
        if (editorRef.current) {
            // Wait for CSS transition to complete (300ms) then trigger layout
            setTimeout(() => {
                editorRef.current?.layout();
            }, 350);
        }
    }, [isHelpPanelOpen, isCodePanelOpen]);

    useEffect(() => {
        if (isCodePanelOpen) {
            fetchCode();
        }
    }, [isCodePanelOpen, eventStore, namespace, value]);

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
            automaticLayout: false,
            scrollBeyondLastLine: false,
            fontSize: 14,
            lineNumbers: 'on',
            renderLineHighlight: 'all',
            tabSize: 2,
            hover: {
                above: false, // Always show hover below to prevent clipping at top
            },
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
            monaco.editor.setModelMarkers(model, 'projection-dsl', markers);
        } else {
            monaco.editor.setModelMarkers(model, 'projection-dsl', []);
        }
    }, [errors]);

    return (
        <div style={{ position: 'relative', height, width: '100%', display: 'flex', overflow: 'hidden' }}>
            <div style={{ flex: 1, position: 'relative', height: '100%', minWidth: 0, overflow: 'hidden' }}>
                <Button
                    icon="pi pi-code"
                    tooltip="View Generated C# Code"
                    tooltipOptions={{ position: 'left' }}
                    onClick={() => setIsCodePanelOpen(!isCodePanelOpen)}
                    className="p-button-rounded p-button-text p-button-lg"
                    style={{
                        position: 'absolute',
                        top: '8px',
                        right: '60px',
                        zIndex: 1000,
                    }}
                    pt={{
                        icon: { style: { fontSize: '1.5rem' } },
                        root: { style: { width: '3rem', height: '3rem' } }
                    }}
                />
                <Button
                    icon="pi pi-question-circle"
                    tooltip={Strings.components.projectionEditor.tooltips.help}
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
                        root: { style: { width: '3rem', height: '3rem' } }
                    }}
                />
                <div ref={containerRef} style={{ height: '100%', width: '100%' }} />
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
                    <h3 style={{ margin: 0, color: '#ffffff', fontSize: '1.1rem' }}>Projection DSL Reference</h3>
                    <Button
                        icon="pi pi-times"
                        onClick={() => setIsHelpPanelOpen(false)}
                        className="p-button-rounded p-button-text"
                        style={{ color: '#cccccc' }}
                    />
                </div>
                <div style={{ flex: 1, overflow: 'auto' }}>
                    <ProjectionHelpPanel />
                </div>
            </div>

            <div
                style={{
                    width: isCodePanelOpen ? '600px' : '0',
                    height: '100%',
                    overflow: 'hidden',
                    transition: 'width 0.3s ease-in-out',
                    borderLeft: isCodePanelOpen ? '1px solid #3e3e42' : 'none',
                    display: isCodePanelOpen ? 'flex' : 'none',
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
                    <h3 style={{ margin: 0, color: '#ffffff', fontSize: '1.1rem' }}>Generated C# Code</h3>
                    <Button
                        icon="pi pi-times"
                        onClick={() => setIsCodePanelOpen(false)}
                        className="p-button-rounded p-button-text"
                        style={{ color: '#cccccc' }}
                    />
                </div>
                <div style={{ flex: 1, overflow: 'hidden' }}>
                    <ProjectionCodePanel
                        declarativeCode={declarativeCode}
                        modelBoundCode={modelBoundCode}
                        onRefresh={fetchCode}
                    />
                </div>
            </div>
        </div>
    );
};
