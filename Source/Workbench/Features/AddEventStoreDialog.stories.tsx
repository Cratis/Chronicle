// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter } from 'react-router-dom';
import { AddEventStoreDialog } from './AddEventStoreDialog';
import { useDialog } from '@cratis/arc.react/dialogs';
import { useEffect } from 'react';

const AddEventStoreDialogStory = () => {
    const [DialogWrapper, showDialog] = useDialog(AddEventStoreDialog);

    useEffect(() => {
        void showDialog();
    }, [showDialog]);

    return <DialogWrapper />;
};

const meta: Meta<typeof AddEventStoreDialog> = {
    title: 'Features/Home/AddEventStoreDialog',
    component: AddEventStoreDialog,
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
type Story = StoryObj<typeof AddEventStoreDialog>;

export const Default: Story = {
    render: () => <AddEventStoreDialogStory />,
};
