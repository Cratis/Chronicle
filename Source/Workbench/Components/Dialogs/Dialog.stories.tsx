// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { DialogComponents } from '@cratis/arc.react/dialogs';
import { BusyIndicatorDialog, ConfirmationDialog } from '@cratis/components/Dialogs';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { Dialog } from './Dialog';

const meta: Meta<typeof Dialog> = {
    title: 'Components/Dialog',
    component: Dialog,
    decorators: [
        (Story) => (
            <DialogComponents confirmation={ConfirmationDialog} busyIndicator={BusyIndicatorDialog}>
                <Story />
            </DialogComponents>
        ),
    ],
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof Dialog>;

export const OkCancel: Story = {
    args: {
        title: 'Example Dialog',
        visible: true,
        onClose: (_result: DialogResult) => {},
        children: <div className="p-4">This is dialog content</div>,
    },
};

export const OkOnly: Story = {
    args: {
        title: 'Confirmation',
        visible: true,
        onClose: (_result: DialogResult) => {},
        children: <p>Are you sure you want to proceed?</p>,
        buttons: 1, // DialogButtons.Ok
    },
};

export const YesNo: Story = {
    args: {
        title: 'Delete Item',
        visible: true,
        onClose: (_result: DialogResult) => {},
        children: <p>Do you want to delete this item? This action cannot be undone.</p>,
        buttons: 3, // DialogButtons.YesNo
    },
};
