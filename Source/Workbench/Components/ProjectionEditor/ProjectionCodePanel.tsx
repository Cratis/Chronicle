// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState } from 'react';
import Editor from '@monaco-editor/react';
import { SelectButton } from 'primereact/selectbutton';
import { Button } from 'primereact/button';

interface ProjectionCodePanelProps {
    declarativeCode: string;
    modelBoundCode: string;
    onRefresh?: () => void;
}

export const ProjectionCodePanel: React.FC<ProjectionCodePanelProps> = ({ declarativeCode, modelBoundCode, onRefresh }) => {
    const [codeType, setCodeType] = useState<'declarative' | 'modelBound'>('declarative');

    const codeTypeOptions = [
        { label: 'Declarative', value: 'declarative' },
        { label: 'Model-Bound', value: 'modelBound' }
    ];

    const handleCopyToClipboard = async () => {
        const code = codeType === 'declarative' ? declarativeCode : modelBoundCode;
        if (code) {
            await navigator.clipboard.writeText(code);
        }
    };

    const currentCode = codeType === 'declarative' ? declarativeCode : modelBoundCode;

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
                <Button
                    icon="pi pi-copy"
                    onClick={handleCopyToClipboard}
                    tooltip="Copy to Clipboard"
                    tooltipOptions={{ position: 'left' }}
                    className="p-button-rounded p-button-text"
                />
            </div>
            <div
                style={{
                    flex: 1,
                    border: '1px solid #3e3e42',
                    borderRadius: '4px',
                    overflow: 'hidden'
                }}
            >
                <Editor
                    height="100%"
                    language="csharp"
                    value={currentCode || '// Loading...'}
                    theme="vs-dark"
                    options={{
                        readOnly: true,
                        minimap: { enabled: false },
                        scrollBeyondLastLine: false,
                        fontSize: 13,
                        lineNumbers: 'on',
                        renderLineHighlight: 'none',
                    }}
                />
            </div>
        </div>
    );
};
