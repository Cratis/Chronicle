// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState } from 'react';
import MonacoEditor from 'Components/MonacoEditor/MonacoEditor';
import { ToggleButton } from 'primereact/togglebutton';
import { ToggleButtonGroup, type ToggleButtonGroupValueChangeEvent } from 'primereact/togglebuttongroup';
import { Button } from '@cratis/components/Common';
import { Dropdown } from '@cratis/components/Dropdown';
import { ProjectionCodeLanguages, editorLanguageFor, supportsModelBound, type ProjectionCodeLanguage } from './ProjectionCodeLanguages';

interface ProjectionCodePanelProps {
    declarativeCode: string;
    modelBoundCode: string;
    language: ProjectionCodeLanguage;
    onLanguageChange: (language: ProjectionCodeLanguage) => void;
    onRefresh?: () => void;
}

export const ProjectionCodePanel: React.FC<ProjectionCodePanelProps> = ({ declarativeCode, modelBoundCode, language, onLanguageChange, onRefresh }) => {
    const [codeType, setCodeType] = useState<'declarative' | 'modelBound'>('declarative');

    // A client without a model-bound projection API has nothing to show for that style, so the view
    // falls back to the one it does have rather than leaving the panel on an empty tab.
    const modelBoundAvailable = supportsModelBound(language);
    const effectiveCodeType = modelBoundAvailable ? codeType : 'declarative';

    const handleCopyToClipboard = async () => {
        const code = effectiveCodeType === 'declarative' ? declarativeCode : modelBoundCode;
        if (code) {
            await navigator.clipboard.writeText(code);
        }
    };

    const currentCode = effectiveCodeType === 'declarative' ? declarativeCode : modelBoundCode;

    return (
        <div style={{ padding: '20px', height: '100%', display: 'flex', flexDirection: 'column', backgroundColor: '#1e1e1e' }}>
            <div style={{ marginBottom: '15px', display: 'flex', gap: '10px', alignItems: 'center' }}>
                <Dropdown<ProjectionCodeLanguage>
                    value={language}
                    options={ProjectionCodeLanguages}
                    optionLabel='label'
                    optionValue='value'
                    aria-label='Language'
                    onChange={value => onLanguageChange(value)}
                    style={{ minWidth: '9rem' }} />
                <ToggleButtonGroup
                    value={effectiveCodeType}
                    allowEmpty={false}
                    onValueChange={(event: ToggleButtonGroupValueChangeEvent) => setCodeType(event.value as 'declarative' | 'modelBound')}
                    style={{ flex: 1 }}
                >
                    <ToggleButton.Root value='declarative'>
                        <ToggleButton.Indicator>Declarative</ToggleButton.Indicator>
                    </ToggleButton.Root>
                    <ToggleButton.Root value='modelBound' disabled={!modelBoundAvailable}>
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
                    language={editorLanguageFor(language)}
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
