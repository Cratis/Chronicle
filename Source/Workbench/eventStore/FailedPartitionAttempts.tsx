// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FormControl, InputLabel, MenuItem, Select, Stack, TextField, Typography } from '@mui/material';
import { FailedPartition } from 'API/events/store/failed-partitions/FailedPartition';
import { FailedPartitionAttempt } from 'API/events/store/failed-partitions/FailedPartitionAttempt';
import { useEffect, useState } from 'react';
import { ObserverInformation } from 'API/events/store/observers/ObserverInformation';

export interface FailedPartitionAttemptsProps {
    failedPartition: FailedPartition;
    observer: ObserverInformation;
}

export const FailedPartitionAttempts = (props: FailedPartitionAttemptsProps) => {
    const [selectedAttempt, setSelectedAttempt] = useState<FailedPartitionAttempt>(props.failedPartition.attempts[0])
    const [selectedAttemptIndex, setSelectedAttemptIndex] = useState<number>(0);

    useEffect(() => {
        setSelectedAttempt(props.failedPartition.attempts[selectedAttemptIndex]);
    }, [selectedAttemptIndex]);

    return (
        <>
            <div>
                <FormControl size="small" sx={{ m: 1, minWidth: 120 }}>
                    <InputLabel>Attempt</InputLabel>
                    <Select
                        label="Attempt"
                        autoWidth
                        value={selectedAttemptIndex}
                        onChange={e => {
                            const selected = Number.parseInt(e.target.value as string);
                            setSelectedAttemptIndex(selected);
                        }}>

                        {props.failedPartition.attempts.map((attempt, index) => {
                            return (
                                <MenuItem key={index} value={index.toString()}>{attempt.occurred.toLocaleString()}</MenuItem>
                            );
                        })}
                    </Select>
                </FormControl>
            </div>
            {selectedAttempt &&
                <>
                    <Typography variant='h6'>SequenceNumber</Typography>
                    <TextField disabled defaultValue={selectedAttempt.sequenceNumber} title={selectedAttempt.sequenceNumber.toString()} style={{ width: '100%' }} />
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
