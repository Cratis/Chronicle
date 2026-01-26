// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useRef, useState } from 'react';
import Editor, { OnMount, Monaco } from '@monaco-editor/react';
import type { editor, Uri } from 'monaco-editor';
import {
    registerProjectionDslLanguage,
    setReadModelSchemas,
    setEventSchemas,
    setEventSequences,
    languageId,
    disposeProjectionDslLanguage,
} from './index';
import { JsonSchema } from 'Components/JsonSchema';
import { ProjectionDefinitionSyntaxError, GenerateDeclarativeCode, GenerateModelBoundCode } from 'Api/Projections';
import { AllEventSequences } from 'Api/EventSequences';
import { Button } from 'primereact/button';
import { ProjectionHelpPanel } from './ProjectionHelpPanel';
import { ProjectionCodePanel } from './ProjectionCodePanel';
import Strings from 'Strings';

export interface ProjectionEditorProps {
    value: string;
    onChange?: (value: string) => void;
    onValidationChange?: (hasErrors: boolean) => void;
    readModelSchemas?: JsonSchema[],
    eventSchemas?: JsonSchema[],
    errors?: ProjectionDefinitionSyntaxError[];
    theme?: string;
    eventStore?: string;
    namespace?: string;
}

export const ProjectionEditor: React.FC<ProjectionEditorProps> = ({
    value,
    onChange,
    onValidationChange,
    readModelSchemas,
    eventSchemas,
    errors,
    theme = 'vs-dark',
    eventStore,
    namespace,
}) => {
    const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null);
    const monacoRef = useRef<Monaco | null>(null);
    const [isHelpPanelOpen, setIsHelpPanelOpen] = useState(false);
    const [isCodePanelOpen, setIsCodePanelOpen] = useState(false);
    const [declarativeCode, setDeclarativeCode] = useState('');
    const [modelBoundCode, setModelBoundCode] = useState('');
    const [generateDeclarativeCode] = GenerateDeclarativeCode.use();
    const [generateModelBoundCode] = GenerateModelBoundCode.use();
    const [allEventSequencesResult] = AllEventSequences.use(eventStore ? { eventStore } : undefined);

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

            const declCode = declarativeResult.response?.code || '// Unable to generate code';
            const modelCode = modelBoundResult.response?.code || '// Unable to generate code';

            setDeclarativeCode(declCode);
            setModelBoundCode(modelCode);
        }
    };

    const handleEditorDidMount: OnMount = (editor, monacoInstance) => {
        editorRef.current = editor;
        monacoRef.current = monacoInstance;
        registerProjectionDslLanguage(monacoInstance);

        // Listen for marker changes to track validation state
        const markerDisposable = monacoInstance.editor.onDidChangeMarkers((resources: readonly Uri[]) => {
            const model = editor.getModel();
            if (model && resources.some((r: Uri) => r.toString() === model.uri.toString())) {
                const markers = monacoInstance.editor.getModelMarkers({ resource: model.uri });
                const hasErrors = markers.some((m: editor.IMarkerData) => m.severity === monacoInstance.MarkerSeverity.Error);
                onValidationChange?.(hasErrors);
            }
        });

        // Initial validation state
        const model = editor.getModel();
        if (model) {
            const markers = monacoInstance.editor.getModelMarkers({ resource: model.uri });
            const hasErrors = markers.some((m: editor.IMarkerData) => m.severity === monacoInstance.MarkerSeverity.Error);
            onValidationChange?.(hasErrors);
        }

        // Clean up on unmount
        return () => {
            markerDisposable.dispose();
        };
    };

    useEffect(() => {
        if (isCodePanelOpen) {
            fetchCode();
        }
    }, [isCodePanelOpen, eventStore, namespace, value]);

    useEffect(() => {
        return () => {
            disposeProjectionDslLanguage();
        };
    }, []);

    // Update schema when it changes
    useEffect(() => {
        if (readModelSchemas) {
            const readModelInfos = readModelSchemas.map(schema => ({
                displayName: schema.title || 'Unknown',
                schema
            }));
            setReadModelSchemas(readModelInfos);
        }
        if (eventSchemas) {
            setEventSchemas(eventSchemas);
        }
    }, [readModelSchemas, eventSchemas]);

    // Update event sequences when they're loaded
    useEffect(() => {
        if (allEventSequencesResult.data) {
            setEventSequences(allEventSequencesResult.data);
        }
    }, [allEventSequencesResult.data]);

    // Update markers when errors change
    useEffect(() => {
        if (!editorRef.current || !monacoRef.current) return;

        const model = editorRef.current.getModel();
        if (!model) return;

        if (errors && errors.length > 0) {
            const markers: editor.IMarkerData[] = errors.map(error => {
                const line = model.getLineContent(error.line);
                const endColumn = Math.max(error.column + 1, line.length);
                return {
                    severity: monacoRef.current!.MarkerSeverity.Error,
                    startLineNumber: error.line,
                    startColumn: error.column,
                    endLineNumber: error.line,
                    endColumn: endColumn,
                    message: error.message,
                };
            });
            monacoRef.current.editor.setModelMarkers(model, 'projection-dsl', markers);
        } else {
            monacoRef.current.editor.setModelMarkers(model, 'projection-dsl', []);
        }
    }, [errors]);

    return (
        <div style={{ position: 'relative', height: '100%', width: '100%', display: 'flex', overflow: 'hidden' }}>
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
                <Editor
                    height="100%"
                    language={languageId}
                    value={value}
                    theme={theme}
                    onChange={(newValue) => onChange?.(newValue || '')}
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
