// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useRef, useState } from 'react';
import MonacoEditor, { Monaco, OnMount } from 'Components/MonacoEditor/MonacoEditor';
import type { editor, Uri } from 'monaco-editor';
import {
    registerProjectionDefinitionLanguage,
    setReadModelSchemas,
    setEventSchemas,
    setEventSequences,
    languageId,
    disposeProjectionDefinitionLanguage,
} from './index';
import type { JsonSchema } from '@cratis/screenplay-language/projection';
import { ProjectionDeclarationSyntaxError, GenerateDeclarativeCode, GenerateModelBoundCode, DraftReadModel } from 'Features/ProjectionEditor';
import { AllEventSequences } from 'Features/Sequences';
import { Button } from '@cratis/components/Common';
import { ProjectionHelpPanel } from './ProjectionHelpPanel';
import { ProjectionCodePanel } from './ProjectionCodePanel';
import { graphProjectionLanguageId, isGraphDeclaration, registerGraphProjectionLanguage } from './graphDeclaration';
import type { ProjectionCodeLanguage } from './ProjectionCodeLanguages';
import { CodeGenerationRequests, generateCode, type GenerationContext } from './codeGenerationRequests';
import Strings from 'Strings';
import type { ReadModelDefinition } from 'Features/ReadModelDefinitions';

/** The owner of the markers the projection language validates a declaration with. */
const projectionValidatorOwner = 'pdl-validator';

export interface ProjectionEditorProps {
    value: string;
    originalValue?: string;
    onChange?: (value: string) => void;
    onValidationChange?: (hasErrors: boolean) => void;
    readModels?: ReadModelDefinition[];
    readModelSchemas?: JsonSchema[];
    eventSchemas?: JsonSchema[] | Record<string, JsonSchema>,
    errors?: ProjectionDeclarationSyntaxError[];
    theme?: string;
    eventStore?: string;
    namespace?: string;
    draftReadModel?: DraftReadModel | null;
    normalizeDeclarationForRequests?: (declaration: string) => string;
}

export const ProjectionEditor: React.FC<ProjectionEditorProps> = ({
    value,
    originalValue,
    onChange,
    onValidationChange,
    readModels,
    readModelSchemas,
    eventSchemas,
    errors,
    theme = 'vs-dark',
    eventStore,
    namespace,
    draftReadModel,
    normalizeDeclarationForRequests,
}) => {
    const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null);
    const monacoRef = useRef<Monaco | null>(null);
    const decorationsRef = useRef<string[]>([]);
    const [isHelpPanelOpen, setIsHelpPanelOpen] = useState(false);
    const [isCodePanelOpen, setIsCodePanelOpen] = useState(false);
    const [codeLanguage, setCodeLanguage] = useState<ProjectionCodeLanguage>('CSharp');
    const [declarativeCode, setDeclarativeCode] = useState('');
    const [modelBoundCode, setModelBoundCode] = useState('');
    const [generateDeclarativeCode] = GenerateDeclarativeCode.use();
    const [generateModelBoundCode] = GenerateModelBoundCode.use();
    const [allEventSequencesResult] = AllEventSequences.use(eventStore ? { eventStore } : undefined);

    // The projection language does not know graph declarations and would mark every line of one as an error,
    // so a graph declaration is edited in its own language, validated by the server when it is previewed or saved.
    const isGraph = isGraphDeclaration(value);
    const isGraphRef = useRef(isGraph);
    isGraphRef.current = isGraph;

    const reportValidity = (editorInstance: editor.IStandaloneCodeEditor, monacoInstance: Monaco) => {
        const model = editorInstance.getModel();
        if (!model) return;
        const markers = monacoInstance.editor.getModelMarkers({ resource: model.uri });
        const hasErrors = markers.some((m: editor.IMarker) =>
            m.severity === monacoInstance.MarkerSeverity.Error && !(isGraphRef.current && m.owner === projectionValidatorOwner));
        onValidationChange?.(hasErrors);
    };

    const applyDeclarationKind = (editorInstance: editor.IStandaloneCodeEditor, monacoInstance: Monaco) => {
        const model = editorInstance.getModel();
        if (!model) return;
        const wanted = isGraphRef.current ? graphProjectionLanguageId : languageId;
        if (model.getLanguageId() !== wanted) {
            monacoInstance.editor.setModelLanguage(model, wanted);
        }
        if (isGraphRef.current) {
            monacoInstance.editor.setModelMarkers(model, projectionValidatorOwner, []);
        }
        reportValidity(editorInstance, monacoInstance);
    };

    const generation = useRef<CodeGenerationRequests | null>(null);
    generation.current ??= new CodeGenerationRequests(
        context => generateCode(generateDeclarativeCode, generateModelBoundCode, context),
        code => {
            setDeclarativeCode(code?.declarative ?? '');
            setModelBoundCode(code?.modelBound ?? '');
        });

    // A snapshot of what code is generated for, or null when there is nothing to generate: graph declarations produce no read model code.
    const currentGenerationContext = (language: ProjectionCodeLanguage = codeLanguage): GenerationContext | null => {
        if (!eventStore || !namespace || !value || isGraph) return null;
        return {
            eventStore,
            namespace,
            declaration: normalizeDeclarationForRequests ? normalizeDeclarationForRequests(value) : value,
            language,
            draftReadModel,
        };
    };

    const fetchCode = (language: ProjectionCodeLanguage = codeLanguage) => {
        const context = currentGenerationContext(language);
        if (context) {
            generation.current?.run(context);
        } else {
            generation.current?.clear();
        }
    };

    const handleEditorDidMount: OnMount = (editor, monacoInstance) => {
        editorRef.current = editor;
        monacoRef.current = monacoInstance;
        registerProjectionDefinitionLanguage(monacoInstance);
        registerGraphProjectionLanguage(monacoInstance);

        // Listen for marker changes to track validation state
        const markerDisposable = monacoInstance.editor.onDidChangeMarkers((resources: readonly Uri[]) => {
            const model = editor.getModel();
            if (model && resources.some((r: Uri) => r.toString() === model.uri.toString())) {
                reportValidity(editor, monacoInstance);
            }
        });

        // Initial validation state
        applyDeclarationKind(editor, monacoInstance);

        // Clean up on unmount
        return () => {
            markerDisposable.dispose();
        };
    };

    // Opening generates immediately, and closing discards whatever was generated or is on its way.
    useEffect(() => {
        if (isCodePanelOpen) {
            fetchCode();
        } else {
            generation.current?.clear();
        }
    }, [isCodePanelOpen]);

    // Edits and context changes coalesce into one request once the declaration has rested.
    useEffect(() => {
        if (!isCodePanelOpen) return;
        const context = currentGenerationContext();
        if (context) {
            generation.current?.schedule(context);
        } else {
            generation.current?.clear();
        }
    }, [eventStore, namespace, value, isGraph, draftReadModel]);

    useEffect(() => () => generation.current?.cancel(), []);

    useEffect(() => {
        return () => {
            disposeProjectionDefinitionLanguage();
        };
    }, []);

    // Follow the declaration between a projection and a graph projection
    useEffect(() => {
        if (editorRef.current && monacoRef.current) {
            applyDeclarationKind(editorRef.current, monacoRef.current);
        }
    }, [isGraph, value]);

    // Update schema when it changes
    useEffect(() => {
        if (readModels) {
            const readModelInfos = readModels.map(readModel => ({
                identifier: readModel.identifier,
                displayName: readModel.displayName,
                schema: JSON.parse(readModel.schema) as JsonSchema
            }));
            setReadModelSchemas(readModelInfos);
        } else if (readModelSchemas) {
            const readModelInfos = readModelSchemas.map(schema => ({
                identifier: schema.title || 'Unknown',
                displayName: schema.title || 'Unknown',
                schema
            }));
            setReadModelSchemas(readModelInfos);
        }
        if (eventSchemas) {
            setEventSchemas(eventSchemas);
        }
    }, [readModels, readModelSchemas, eventSchemas]);

    // Update event sequences when they're loaded
    useEffect(() => {
        if (allEventSequencesResult.data) {
            setEventSequences(allEventSequencesResult.data.map(eventSequence => eventSequence.name));
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
            monacoRef.current.editor.setModelMarkers(model, 'projection-declaration-server', markers);
        } else {
            monacoRef.current.editor.setModelMarkers(model, 'projection-declaration-server', []);
        }
    }, [errors]);

    // Update modified line decorations when value or originalValue changes
    useEffect(() => {
        if (!editorRef.current || !monacoRef.current) return;

        const editor = editorRef.current;
        const model = editor.getModel();
        if (!model) return;

        // If no original value, clear decorations
        if (originalValue === undefined || originalValue === value) {
            decorationsRef.current = editor.deltaDecorations(decorationsRef.current, []);
            return;
        }

        // Calculate which lines have changed
        const originalLines = originalValue.split('\n');
        const currentLines = value.split('\n');
        const decorations: editor.IModelDeltaDecoration[] = [];

        const maxLines = Math.max(originalLines.length, currentLines.length);

        for (let i = 0; i < maxLines; i++) {
            const originalLine = originalLines[i] ?? '';
            const currentLine = currentLines[i] ?? '';

            if (i >= currentLines.length) {
                // Line was deleted - we can't show decoration for non-existent lines
                continue;
            }

            if (i >= originalLines.length) {
                // New line added
                decorations.push({
                    range: { startLineNumber: i + 1, startColumn: 1, endLineNumber: i + 1, endColumn: 1 },
                    options: {
                        isWholeLine: true,
                        linesDecorationsClassName: 'modified-line-decoration added-line',
                        overviewRuler: {
                            color: '#587c0c',
                            position: monacoRef.current!.editor.OverviewRulerLane.Left
                        }
                    }
                });
            } else if (originalLine !== currentLine) {
                // Line was modified
                decorations.push({
                    range: { startLineNumber: i + 1, startColumn: 1, endLineNumber: i + 1, endColumn: 1 },
                    options: {
                        isWholeLine: true,
                        linesDecorationsClassName: 'modified-line-decoration modified-line',
                        overviewRuler: {
                            color: '#0c7d9d',
                            position: monacoRef.current!.editor.OverviewRulerLane.Left
                        }
                    }
                });
            }
        }

        decorationsRef.current = editor.deltaDecorations(decorationsRef.current, decorations);
    }, [value, originalValue]);

    return (
        <div style={{ position: 'relative', height: '100%', width: '100%', display: 'flex', overflow: 'hidden' }}>
            <div style={{ flex: 1, position: 'relative', height: '100%', minWidth: 0, overflow: 'hidden' }}>
                {!isGraph && <Button
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
                />}
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
                <MonacoEditor
                    height="100%"
                    language={languageId}
                    value={value}
                    theme={theme}
                    onChange={(newValue) => {
                        // Clear server-side validation errors when user edits
                        if (editorRef.current && monacoRef.current) {
                            const model = editorRef.current.getModel();
                            if (model) {
                                monacoRef.current.editor.setModelMarkers(model, 'projection-declaration-server', []);
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
                    <h3 style={{ margin: 0, color: '#ffffff', fontSize: '1.1rem' }}>{Strings.components.projectionEditor.languageReference}</h3>
                    <Button
                        icon="pi pi-times"
                        onClick={() => setIsHelpPanelOpen(false)}
                        shape='pill' variant='ghost'
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
                        shape='pill' variant='ghost'
                        style={{ color: '#cccccc' }}
                    />
                </div>
                <div style={{ flex: 1, overflow: 'hidden' }}>
                    <ProjectionCodePanel
                        declarativeCode={declarativeCode}
                        modelBoundCode={modelBoundCode}
                        language={codeLanguage}
                        onLanguageChange={language => {
                            setCodeLanguage(language);
                            fetchCode(language);
                        }}
                        onRefresh={() => fetchCode()}
                    />
                </div>
            </div>
        </div>
    );
};
