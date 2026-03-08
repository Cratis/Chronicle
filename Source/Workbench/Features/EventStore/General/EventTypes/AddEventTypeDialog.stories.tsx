// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { AddEventTypeDialog } from './AddEventTypeDialog';
import { useDialog } from '@cratis/arc.react/dialogs';
import { useEffect } from 'react';

const AddEventTypeDialogStory = () => {
    const [DialogWrapper, showDialog] = useDialog(AddEventTypeDialog);

    useEffect(() => {
        void showDialog();
    }, [showDialog]);

    return <DialogWrapper />;
};

const meta: Meta<typeof AddEventTypeDialog> = {
    title: 'Features/EventStore/General/EventTypes/AddEventTypeDialog',
    component: AddEventTypeDialog,
    decorators: [
        (Story) => (
            <MemoryRouter initialEntries={['/event-store/my-store/default']}>
                <Routes>
                    <Route path='/event-store/:eventStore/:namespace/*' element={<Story />} />
                </Routes>
            </MemoryRouter>
        ),
    ],
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof AddEventTypeDialog>;

export const Default: Story = {
    render: () => <AddEventTypeDialogStory />,
};
