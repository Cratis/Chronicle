// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter } from 'react-router-dom';
import { useEffect } from 'react';
import { useDialog } from '@cratis/arc.react/dialogs';
import { BindingDialog, BindingDialogRequest } from './BindingDialog';

const BindingDialogStory = () => {
    const [DialogWrapper, showDialog] = useDialog<object, BindingDialogRequest>(BindingDialog);

    useEffect(() => {
        void showDialog({});
    }, [showDialog]);

    return <DialogWrapper />;
};

const meta: Meta<typeof BindingDialog> = {
    title: 'Features/EventStore/System/Federation/BindingDialog',
    component: BindingDialog,
    decorators: [
        (Story) => (
            <MemoryRouter>
                <Story />
            </MemoryRouter>
        ),
    ],
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof BindingDialog>;

export const Default: Story = {
    render: () => <BindingDialogStory />,
};
