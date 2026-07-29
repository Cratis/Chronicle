// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react';
import { fn } from 'storybook/test';
import { CaptureEditor } from './CaptureEditor';

const sampleCapture = `capture InvoiceCapture
    source api
        api InvoicingService
        route /invoices
        poll 5m
    key id
    map
        status = status translate
            "utkast" => draft
            "betalt" => paid
    append InvoiceStatusChanged
        when status
`;

const meta = {
    title: 'Components/CaptureEditor',
    component: CaptureEditor,
    parameters: { layout: 'fullscreen' },
    tags: ['autodocs'],
} satisfies Meta<typeof CaptureEditor>;

export default meta;
type Story = StoryObj<typeof meta>;

/** The Capture Declaration Language editor with syntax highlighting, completions, hover, and validation from `@cratis/screenplay-language/capture`. */
export const Playground: Story = {
    args: {
        value: sampleCapture,
        onChange: fn(),
        onValidationChange: fn(),
    },
    render: (args) => (
        <div style={{ height: '600px' }}>
            <CaptureEditor {...args} />
        </div>
    ),
};

const Demo = () => {
    const [value, setValue] = useState(sampleCapture);
    const [hasErrors, setHasErrors] = useState(false);

    return (
        <div style={{ height: '600px', display: 'flex', flexDirection: 'column' }}>
            <div style={{ padding: '0.5rem', color: hasErrors ? 'crimson' : 'seagreen' }}>
                {hasErrors ? 'Validation errors present' : 'Valid capture declaration'}
            </div>
            <div style={{ flex: 1 }}>
                <CaptureEditor value={value} onChange={setValue} onValidationChange={setHasErrors} />
            </div>
        </div>
    );
};

/** Live validation state driven by the editor's `onValidationChange` callback. */
export const Interactive: Story = {
    args: {
        value: sampleCapture,
    },
    render: () => <Demo />,
};
