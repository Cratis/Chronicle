// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useRef, useEffect, useState } from 'react';
import * as monaco from 'monaco-editor';
import { SelectButton } from 'primereact/selectbutton';
import { Button } from 'primereact/button';

interface ProjectionCodePanelProps {
    declarativeCode: string;
    modelBoundCode: string;
    onRefresh?: () => void;
}

export const ProjectionCodePanel: React.FC<ProjectionCodePanelProps> = ({ declarativeCode, modelBoundCode, onRefresh }) => {
    const editorRef = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);
    const containerRef = useRef<HTMLDivElement>(null);
    const [codeType, setCodeType] = useState<'declarative' | 'modelBound'>('declarative');

    const codeTypeOptions = [
        { label: 'Declarative', value: 'declarative' },
        { label: 'Model-Bound', value: 'modelBound' }
    ];

    useEffect(() => {
        if (!containerRef.current) return;

        editorRef.current = monaco.editor.create(containerRef.current, {
            value: declarativeCode || '// Loading...',
            language: 'csharp',
            theme: 'vs-dark',
            readOnly: true,
            minimap: { enabled: false },
            automaticLayout: true,
            scrollBeyondLastLine: false,
            fontSize: 13,
            lineNumbers: 'on',
            renderLineHighlight: 'none',
        });

        return () => {
            editorRef.current?.dispose();
        };
    }, []);

    useEffect(() => {
        if (editorRef.current) {
            const code = codeType === 'declarative' ? declarativeCode : modelBoundCode;
            if (code) {
                editorRef.current.setValue(code);
            }
        }
    }, [codeType, declarativeCode, modelBoundCode]);

    return (
        <div style={{ padding: '20px', height: '100%', display: 'flex', flexDirection: 'column', backgroundColor: '#1e1e1e' }}>
            <div style={{ marginBottom: '15px', display: 'flex', gap: '10px', alignItems: 'center' }}>
                <SelectButton
                    value={codeType}
                    onChange={(e) => setCodeType(e.value)}
                    options={codeTypeOptions}
                    style={{ flex: 1 }}
                />
                <Button
                    icon="pi pi-refresh"
                    onClick={onRefresh}
                    disabled={!onRefresh}
                    tooltip="Refresh Code"
                    tooltipOptions={{ position: 'left' }}
                    className="p-button-rounded p-button-text"
                />
            </div>
            <div
                ref={containerRef}
                style={{
                    flex: 1,
                    border: '1px solid #3e3e42',
                    borderRadius: '4px',
                    overflow: 'hidden'
                }}
            />
        </div>
    );
};
