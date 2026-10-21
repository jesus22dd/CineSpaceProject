const selects = new WeakMap();
const normalized = text => text.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase('es');

export function connectSelect(root, reference) {
    if (!(root instanceof Element) || !root.isConnected) return;
    if (selects.has(root)) return;
    const trigger = root.querySelector('[role="combobox"]');
    const menu = root.querySelector('[role="listbox"]');
    if (!trigger || !menu) return;
    let active = -1, search = '', lastTyped = 0, disposed = false;
    const options = () => [...menu.querySelectorAll('[role="option"]')];
    const opened = () => menu.matches(':popover-open');
    const position = () => {
        if (!opened()) return;
        const rect = trigger.getBoundingClientRect();
        const below = window.innerHeight - rect.bottom - 12;
        const above = rect.top - 12;
        const upward = below < Math.min(368, menu.scrollHeight) && above > below;
        const width = Math.min(window.innerWidth - 16, Math.max(260, rect.width));
        menu.style.width = `${width}px`;
        menu.style.maxHeight = `${Math.min(368, Math.max(44, upward ? above : below))}px`;
        menu.style.left = `${Math.max(8, Math.min(rect.left, window.innerWidth - width - 8))}px`;
        menu.style.top = `${upward ? Math.max(8, rect.top - Math.min(menu.scrollHeight, parseFloat(menu.style.maxHeight)) - 6) : rect.bottom + 6}px`;
    };
    const activate = index => {
        const items = options();
        active = Math.max(0, Math.min(index, items.length - 1));
        items.forEach((item, i) => item.classList.toggle('is-active', i === active));
        trigger.setAttribute('aria-activedescendant', items[active]?.id ?? '');
        items[active]?.scrollIntoView({block: 'nearest', behavior: 'instant'});
    };
    const open = () => {
        if (!opened()) menu.showPopover();
        position();
        activate(Math.max(0, options().findIndex(item => item.getAttribute('aria-selected') === 'true')));
    };
    const close = () => { if (opened()) menu.hidePopover(); };
    const choose = item => {
        if (!item || disposed) return;
        close();
        trigger.focus({preventScroll: true});
        reference.invokeMethodAsync('Seleccionar', item.dataset.value).catch(() => {});
    };
    const click = () => opened() ? close() : open();
    const select = event => choose(event.target.closest('[role="option"]'));
    const toggle = () => {
        trigger.setAttribute('aria-expanded', String(opened()));
        if (!opened()) { trigger.removeAttribute('aria-activedescendant'); search = ''; }
    };
    const beforeToggle = event => {
        trigger.setAttribute('aria-expanded', String(event.newState === 'open'));
        if (event.newState === 'closed') trigger.removeAttribute('aria-activedescendant');
    };
    const keydown = event => {
        const key = event.key;
        if (key === 'Tab') { close(); return; }
        if (key === 'Escape') { if (opened()) { event.preventDefault(); close(); } return; }
        if (['ArrowDown', 'ArrowUp', 'Home', 'End', 'Enter', ' '].includes(key)) {
            event.preventDefault();
            if (!opened()) { open(); if (key === 'End') activate(options().length - 1); else if (key === 'Home') activate(0); return; }
            if (key === 'Enter' || key === ' ') choose(options()[active]);
            else activate(key === 'Home' ? 0 : key === 'End' ? options().length - 1 : active + (key === 'ArrowDown' ? 1 : -1));
        } else if (key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
            event.preventDefault();
            if (!opened()) open();
            const now = Date.now();
            search = now - lastTyped < 700 ? search + key : key;
            lastTyped = now;
            const index = options().findIndex(item => normalized(item.getAttribute('aria-label') || item.dataset.value).startsWith(normalized(search)));
            if (index >= 0) activate(index);
        }
    };
    const scroll = event => { if (!menu.contains(event.target)) position(); };
    const resize = () => close();
    trigger.addEventListener('click', click);
    root.addEventListener('keydown', keydown);
    menu.addEventListener('click', select);
    menu.addEventListener('toggle', toggle);
    menu.addEventListener('beforetoggle', beforeToggle);
    window.addEventListener('scroll', scroll, true);
    window.addEventListener('resize', resize);
    selects.set(root, () => {
        disposed = true; close();
        trigger.removeEventListener('click', click);
        root.removeEventListener('keydown', keydown);
        menu.removeEventListener('click', select);
        menu.removeEventListener('toggle', toggle);
        menu.removeEventListener('beforetoggle', beforeToggle);
        window.removeEventListener('scroll', scroll, true);
        window.removeEventListener('resize', resize);
    });
}
export function disconnectSelect(root) { selects.get(root)?.(); selects.delete(root); }
