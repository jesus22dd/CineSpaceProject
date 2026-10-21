(() => {
    const key = 'cinespace-theme';
    const preference = matchMedia('(prefers-color-scheme: dark)');
    const subscribers = new Map();
    let nextId = 0;
    let explicit;
    try { explicit = localStorage.getItem(key); } catch { }
    if (explicit !== 'light' && explicit !== 'dark') explicit = null;
    const isDark = () => document.documentElement.dataset.theme === 'dark';
    function apply(theme) {
        document.documentElement.dataset.theme = theme;
        document.querySelector('meta[name="theme-color"]')?.setAttribute('content', theme === 'dark' ? '#111216' : '#642633');
        subscribers.forEach(reference => reference.invokeMethodAsync('ThemeChanged', isDark()).catch(() => {}));
    }
    apply(explicit || (preference.matches ? 'dark' : 'light'));
    preference.addEventListener('change', () => {
        if (!explicit) apply(preference.matches ? 'dark' : 'light');
    });
    window.addEventListener('storage', event => {
        if (event.key !== key && event.key !== null) return;
        explicit = event.newValue === 'dark' || event.newValue === 'light' ? event.newValue : null;
        apply(explicit || (preference.matches ? 'dark' : 'light'));
    });
    window.cineTheme = {
        connect(reference) { const id = ++nextId; subscribers.set(id, reference); return [id, isDark() ? 1 : 0]; },
        disconnect(id) { subscribers.delete(id); },
        toggle() {
            explicit = isDark() ? 'light' : 'dark';
            try { localStorage.setItem(key, explicit); } catch { }
            apply(explicit);
        }
    };
})();
