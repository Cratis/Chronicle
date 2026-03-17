// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter } from 'react-router-dom';
import { Guid } from '@cratis/fundamentals';
import { ChangePasswordDialog } from './ChangePasswordDialog';
import { useDialog } from '@cratis/arc.react/dialogs';
import { useEffect } from 'react';

const ChangePasswordDialogStory = () => {
    const [DialogWrapper, showDialog] = useDialog(ChangePasswordDialog);

    useEffect(() => {
        void showDialog({ userId: Guid.parse('00000000-0000-0000-0000-000000000001') });
    }, [showDialog]);

    return <DialogWrapper />;
};

const meta: Meta<typeof ChangePasswordDialog> = {
    title: 'Features/EventStore/System/Users/ChangePasswordDialog',
    component: ChangePasswordDialog,
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
type Story = StoryObj<typeof ChangePasswordDialog>;

export const Default: Story = {
    render: () => <ChangePasswordDialogStory />,
};
