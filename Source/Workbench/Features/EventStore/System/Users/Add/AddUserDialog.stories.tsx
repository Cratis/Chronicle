// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter } from 'react-router-dom';
import { AddUserDialog } from './AddUserDialog';
import { useDialog } from '@cratis/arc.react/dialogs';
import { useEffect } from 'react';

const AddUserDialogStory = () => {
    const [DialogWrapper, showDialog] = useDialog(AddUserDialog);

    useEffect(() => {
        void showDialog();
    }, [showDialog]);

    return <DialogWrapper />;
};

const meta: Meta<typeof AddUserDialog> = {
    title: 'Features/EventStore/System/Users/AddUserDialog',
    component: AddUserDialog,
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
type Story = StoryObj<typeof AddUserDialog>;

export const Default: Story = {
    render: () => <AddUserDialogStory />,
};
