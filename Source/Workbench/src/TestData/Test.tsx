import { useLoaderData } from 'react-router-dom';
import { Layout } from '../Layout/Layout';

export function Test() {
    const data = useLoaderData();

    return <Layout> {JSON.stringify(data)}</Layout>;
}
