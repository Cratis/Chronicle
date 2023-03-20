// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IModalProps } from '@aksio/cratis-mui';
import { Box, TextField } from '@mui/material';
import { AppendedEventWithJsonAsContent } from 'API/events/store/sequence/AppendedEventWithJsonAsContent';
import { useState } from 'react';

const RedactEventsModal = (props: IModalProps<AppendedEventWithJsonAsContent, AppendedEventWithJsonAsContent>) => {
    const [reason, setReason] = useState('');

    props.onClose(result => {
        const output = {
            ...props.input!
        };
        output.content.reason = reason;
        return output;
    });

    return (
        <Box sx={{ p: 2, width: 400 }}>
            <TextField
                required
                sx={{ width: '100%' }}
                label='Reason' value={reason} onChange={e => setReason(e.currentTarget.value)} />
        </Box>
    );
};

export default RedactEventsModal;