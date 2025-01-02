// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SerializableDateTimeOffset } from 'Api/Primitives';

SerializableDateTimeOffset.prototype.toLocaleString = function () {
    const epochTicks = 621355968000000000; // Ticks at Unix epoch
    const ticksPerMillisecond = 10000; // Ticks per millisecond
    const milliseconds = (this.ticks - epochTicks) / ticksPerMillisecond;
    const date = new Date(milliseconds);
    const offsetInMinutes = this.offsetMinutes / 60000; // Offset is in milliseconds
    const utcDate = new Date(date.getTime() + offsetInMinutes * 60000);
    return utcDate.toLocaleString();
};
