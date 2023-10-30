export async function postLoader() {
    const api_url = 'https://jsonplaceholder.typicode.com/posts';

    let data = null
    try {

        data = await fetch(api_url)

    } catch (err: unknown) {
        throw new Error(JSON.stringify(err));
    }

    return data
}

