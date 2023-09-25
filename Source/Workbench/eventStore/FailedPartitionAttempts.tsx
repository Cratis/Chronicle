// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FormControl, InputLabel, MenuItem, Select, Stack, TextField, Typography } from '@mui/material';
import { FailedPartition } from 'API/events/store/failed-partitions/FailedPartition';
import { FailedPartitionAttempt } from 'API/events/store/failed-partitions/FailedPartitionAttempt';
import { useState } from 'react';
import { ObserverInformation } from 'API/events/store/observers/ObserverInformation';
import { Label } from '@mui/icons-material';

export interface FailedPartitionAttemptsProps {
    failedPartition: FailedPartition;
    observer: ObserverInformation;
}

export const FailedPartitionAttempts = (props: FailedPartitionAttemptsProps) => {
    const [selectedAttempt, setSelectedAttempt] = useState<FailedPartitionAttempt>(props.failedPartition.attempts[0])

    return (
        <>
            <div>
                <FormControl size="small" sx={{ m: 1, minWidth: 120 }}>
                    <InputLabel>Attempt</InputLabel>
                    <Select
                        label="Attempt"
                        autoWidth
                        value={selectedAttempt?.occurred || ''}
                        onChange={e => setSelectedAttempt(props.failedPartition.attempts.find(_ => _.occurred == e.target.value)!)}>

                        {props.failedPartition.attempts.map(attempt => {
                            return (
                                <MenuItem key={attempt.occurred} value={attempt.occurred}>{attempt.occurred.toLocaleString()}</MenuItem>
                            );
                        })}
                    </Select>
                </FormControl>
            </div>
            {selectedAttempt &&
                <>
                    <Typography variant='h6'>Messages</Typography>
                    {
                        (selectedAttempt.messages) && selectedAttempt.messages.map((value, index) =>
                            <TextField key={index} disabled defaultValue={value.toString()} title={value.toString()} style={{ width: '100%' }} />)
                    }
                    <Typography variant='h6'>Stack trace</Typography>
                    <TextField disabled defaultValue={selectedAttempt.stackTrace} multiline title={selectedAttempt.stackTrace.toString()} style={{ width: '100%' }} />
                </>}
        </>
    )
}
