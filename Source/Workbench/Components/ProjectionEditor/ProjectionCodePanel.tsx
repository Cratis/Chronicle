// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState } from 'react';
import MonacoEditor from 'Components/MonacoEditor/MonacoEditor';
import { ToggleButton } from 'primereact/togglebutton';
import { ToggleButtonGroup, type ToggleButtonGroupValueChangeEvent } from 'primereact/togglebuttongroup';
import { Button } from '@cratis/components/Common';

interface ProjectionCodePanelProps {
    declarativeCode: string;
    modelBoundCode: string;
    onRefresh?: () => void;
}

export const ProjectionCodePanel: React.FC<ProjectionCodePanelProps> = ({ declarativeCode, modelBoundCode, onRefresh }) => {
    const [codeType, setCodeType] = useState<'declarative' | 'modelBound'>('declarative');

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
                <ToggleButtonGroup
                    value={codeType}
                    allowEmpty={false}
                    onValueChange={(event: ToggleButtonGroupValueChangeEvent) => setCodeType(event.value as 'declarative' | 'modelBound')}
                    style={{ flex: 1 }}
                >
                    <ToggleButton.Root value='declarative'>
                        <ToggleButton.Indicator>Declarative</ToggleButton.Indicator>
                    </ToggleButton.Root>
                    <ToggleButton.Root value='modelBound'>
                        <ToggleButton.Indicator>Model-Bound</ToggleButton.Indicator>
                    </ToggleButton.Root>
                </ToggleButtonGroup>
                <Button
                    icon="pi pi-refresh"
                    onClick={onRefresh}
                    disabled={!onRefresh}
                    tooltip="Refresh Code"
                    tooltipOptions={{ position: 'left' }}
                    shape='pill' variant='ghost'
                />
                <Button
                    icon="pi pi-copy"
                    onClick={handleCopyToClipboard}
                    tooltip="Copy to Clipboard"
                    tooltipOptions={{ position: 'left' }}
                    shape='pill' variant='ghost'
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
                <MonacoEditor
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
