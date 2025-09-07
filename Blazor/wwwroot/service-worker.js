// h2booking service worker (compat, tolerant install)
const CACHE = 'h2booking-cache-v2';
const ASSET_URLS = [
    '/',               // root
    '/index.html',
    '/manifest.webmanifest',  // <-- matches your link tag
    '/favicon.png',
    '/icon-192.png',
    '/icon-512.png',
    '/css/app.css',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/Blazor.styles.css'
];

// Helper: only same-origin requests are cached
const sameOrigin = (url) => new URL(url).origin === self.location.origin;

self.addEventListener('install', (event) => {
    event.waitUntil((async () => {
        const cache = await caches.open(CACHE);

        // Cache each asset individually so one failure doesn't abort install
        const results = await Promise.allSettled(
            ASSET_URLS.map((u) => cache.add(u))
        );

        const failed = results.filter(r => r.status === 'rejected');
        if (failed.length) {
            // Log but continue (don’t break install)
            console.warn('SW: some assets failed to cache:', failed.length);
        }

        await self.skipWaiting();
    })());
});

self.addEventListener('activate', (event) => {
    event.waitUntil((async () => {
        // Clean old caches
        const keys = await caches.keys();
        await Promise.all(keys.map(k => (k !== CACHE) && caches.delete(k)));
        await self.clients.claim();
    })());
});

self.addEventListener('fetch', (event) => {
    const req = event.request;
    const url = new URL(req.url);

    // Never intercept API calls
    if (url.pathname.startsWith('/api/')) return;

    // Don’t try to cache cross-origin CDN calls (let the network handle those)
    if (!sameOrigin(req.url)) return;

    // Navigation requests: network-first with offline fallback to index.html
    if (req.mode === 'navigate') {
        event.respondWith((async () => {
            try {
                const fresh = await fetch(req);
                return fresh;
            } catch {
                const cache = await caches.open(CACHE);
                const fallback = await cache.match('/index.html');
                return fallback || new Response('Offline', { status: 503 });
            }
        })());
        return;
    }

    // For same-origin GET requests: cache-first, then network & backfill
    if (req.method === 'GET') {
        event.respondWith((async () => {
            const cached = await caches.match(req);
            if (cached) return cached;

            try {
                const res = await fetch(req);
                // Clone and store successful responses
                if (res && res.status === 200 && res.type === 'basic') {
                    const copy = res.clone();
                    const cache = await caches.open(CACHE);
                    cache.put(req, copy);
                }
                return res;
            } catch {
                // As a last resort, give any cached fallback if exists
                return caches.match('/index.html');
            }
        })());
    }
});
