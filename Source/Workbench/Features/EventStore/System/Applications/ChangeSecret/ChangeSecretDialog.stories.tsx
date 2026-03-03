// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter } from 'react-router-dom';
import { Guid } from '@cratis/fundamentals';
import { ChangeSecretDialog } from './ChangeSecretDialog';
import { useDialog } from '@cratis/arc.react/dialogs';
import { useEffect } from 'react';

const ChangeSecretDialogStory = () => {
    const [DialogWrapper, showDialog] = useDialog(ChangeSecretDialog);

    useEffect(() => {
        void showDialog({ applicationId: Guid.parse('00000000-0000-0000-0000-000000000001') });
    }, [showDialog]);

    return <DialogWrapper />;
};

const meta: Meta<typeof ChangeSecretDialog> = {
    title: 'Features/EventStore/System/Applications/ChangeSecretDialog',
    component: ChangeSecretDialog,
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
type Story = StoryObj<typeof ChangeSecretDialog>;

export const Default: Story = {
    render: () => <ChangeSecretDialogStory />,
};
