// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter } from 'react-router-dom';
import { AddApplicationDialog } from './AddApplicationDialog';
import { useDialog } from '@cratis/arc.react/dialogs';
import { useEffect } from 'react';

const AddApplicationDialogStory = () => {
    const [DialogWrapper, showDialog] = useDialog(AddApplicationDialog);

    useEffect(() => {
        void showDialog();
    }, [showDialog]);

    return <DialogWrapper />;
};

const meta: Meta<typeof AddApplicationDialog> = {
    title: 'Features/EventStore/System/Applications/AddApplicationDialog',
    component: AddApplicationDialog,
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
type Story = StoryObj<typeof AddApplicationDialog>;

export const Default: Story = {
    render: () => <AddApplicationDialogStory />,
};
