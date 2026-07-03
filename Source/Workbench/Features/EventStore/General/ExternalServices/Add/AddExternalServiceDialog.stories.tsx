// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { AddExternalServiceDialog } from './AddExternalServiceDialog';
import { useDialog } from '@cratis/arc.react/dialogs';
import { useEffect } from 'react';

const AddExternalServiceDialogStory = () => {
    const [DialogWrapper, showDialog] = useDialog(AddExternalServiceDialog);

    useEffect(() => {
        void showDialog();
    }, [showDialog]);

    return <DialogWrapper />;
};

const meta: Meta<typeof AddExternalServiceDialog> = {
    title: 'Features/EventStore/General/ExternalServices/AddExternalServiceDialog',
    component: AddExternalServiceDialog,
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
type Story = StoryObj<typeof AddExternalServiceDialog>;

export const Default: Story = {
    render: () => <AddExternalServiceDialogStory />,
};
