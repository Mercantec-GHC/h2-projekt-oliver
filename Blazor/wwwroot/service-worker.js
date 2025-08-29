
const CACHE = 'h2booking-cache-v1';
const ASSETS = [
    '/', '/index.html', '/manifest.json'

];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE).then(cache => cache.addAll(ASSETS))
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys => Promise.all(keys.map(k => k !== CACHE && caches.delete(k))))
    );
    self.clients.claim();
});


self.addEventListener('fetch', event => {
    const req = event.request;
    const url = new URL(req.url);


    if (url.pathname.startsWith('/api/')) return;

    if (req.method === 'GET') {
        event.respondWith(
            caches.match(req).then(cached => cached || fetch(req).then(res => {
                const copy = res.clone();
                caches.open(CACHE).then(cache => cache.put(req, copy));
                return res;
            }))
        );
    }
});
