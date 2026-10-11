// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', () => { });

// Tryck på vilotimerns notis: visa appen igen (eller öppna den om den stängts).
self.addEventListener('notificationclick', event => {
    event.notification.close();
    event.waitUntil((async () => {
        const windows = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
        if (windows.length > 0)
            return windows[0].focus();
        return self.clients.openWindow(self.registration.scope);
    })());
});
