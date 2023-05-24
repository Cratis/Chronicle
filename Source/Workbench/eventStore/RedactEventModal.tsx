// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AppendedEventWithJsonAsContent } from 'API/events/store/sequence/AppendedEventWithJsonAsContent';
import { IModalProps } from '@aksio/applications-mui';
import { useState } from 'react';
import { Box, TextField } from '@mui/material';


const RedactEventModal = (props: IModalProps<AppendedEventWithJsonAsContent, AppendedEventWithJsonAsContent>) => {
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

export default RedactEventModal;
