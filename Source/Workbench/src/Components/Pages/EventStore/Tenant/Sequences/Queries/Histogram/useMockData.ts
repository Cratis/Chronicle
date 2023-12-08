import React from 'react';

export const useMockData = (eventLogId: any) => {
    const [data, setData] = React.useState<any>({ data: [] });

    React.useEffect(() => {
        setTimeout(() => {
            const mockData = [];
            const startDate = new Date();
            for (let i = 0; i < 30; i++) {
                const date = new Date(startDate);
                date.setDate(date.getDate() + i);
                mockData.push({
                    date: date.toISOString().split('T')[0],
                    count: Math.floor(Math.random() * 100)
                });
            }
            setData({ data: mockData });
        }, 1000);
    }, [eventLogId]);

    return data;
};
