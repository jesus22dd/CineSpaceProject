const menus = new WeakMap();
export function connectAccountMenu(trigger, panel) {
    if (!trigger?.isConnected || !panel?.isConnected) return;
    const position = () => {
        const rect = trigger.getBoundingClientRect();
        const width = Math.min(parseFloat(getComputedStyle(panel).width) || 288, innerWidth - 24);
        panel.style.left = `${Math.max(12, Math.min(rect.right - width, innerWidth - width - 12))}px`;
        panel.style.top = `${rect.bottom + 12}px`;
    };
    const toggle = event => {
        trigger.setAttribute('aria-expanded', String(event.newState === 'open'));
        if (event.newState === 'open') position();
    };
    const resize = () => { if (panel.matches(':popover-open')) position(); };
    const isMenu = panel.getAttribute('role') === 'menu';
    const menuItems = () => [...panel.querySelectorAll('[role="menuitem"]')].filter(item => !item.matches(':disabled'));
    const menuKey = event => {
        if (!isMenu || !['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return;
        event.preventDefault(); const items = menuItems(), index = items.indexOf(document.activeElement);
        const next = event.key === 'Home' ? items[0] : event.key === 'End' ? items.at(-1) : items[(index + (event.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length];
        next?.focus({preventScroll: true});
    };
    const triggerKey = event => {
        if (!isMenu || !['ArrowDown', 'ArrowUp'].includes(event.key)) return;
        event.preventDefault(); if (!panel.matches(':popover-open')) panel.showPopover();
        const items = menuItems(); (event.key === 'ArrowDown' ? items[0] : items.at(-1))?.focus({preventScroll: true});
    };
    trigger.setAttribute('aria-expanded', 'false');
    panel.addEventListener('beforetoggle', toggle);
    panel.addEventListener('keydown', menuKey);
    trigger.addEventListener('keydown', triggerKey);
    window.addEventListener('resize', resize);
    menus.set(panel, {trigger, toggle, resize, menuKey, triggerKey});
}
export function closeAccountMenu(panel) {
    if (panel?.matches(':popover-open')) {
        panel.hidePopover();
        menus.get(panel)?.trigger.focus({preventScroll: true});
    }
}
export function disconnectAccountMenu(panel) {
    const state = menus.get(panel);
    if (!state) return;
    closeAccountMenu(panel);
    panel.removeEventListener('beforetoggle', state.toggle);
    panel.removeEventListener('keydown', state.menuKey);
    state.trigger.removeEventListener('keydown', state.triggerKey);
    window.removeEventListener('resize', state.resize);
    menus.delete(panel);
}
let nextViewport = 0;
const viewportListeners = new Map();
const viewport = matchMedia('(min-width: 1024px)');
export function setDrawerOpen(open) { document.documentElement.classList.toggle('navigation-drawer-open', open); }
export function getNavigationExpanded() { try { return localStorage.getItem('cine-navigation-expanded') !== 'false'; } catch { return true; } }
export function saveNavigationExpanded(expanded) { try { localStorage.setItem('cine-navigation-expanded', String(expanded)); } catch { } }
export function resetPageScroll() { window.scrollTo({top: 0, behavior: 'instant'}); }

export function connectViewport(reference) {
    const id = ++nextViewport;
    const listener = event => reference.invokeMethodAsync('ViewportChanged', event.matches).catch(() => {});
    viewportListeners.set(id, listener);
    viewport.addEventListener('change', listener);
    return [id, viewport.matches ? 1 : 0];
}
export function disconnectViewport(id) { const listener = viewportListeners.get(id); if (listener) viewport.removeEventListener('change', listener); viewportListeners.delete(id); }
export async function focusNavigation(element, trigger) {
    await new Promise(resolve => requestAnimationFrame(resolve));
    element.querySelector('button, a[href]')?.focus({preventScroll: true});
    if (!element.dataset.focusGuard) {
        element.dataset.focusGuard = 'true';
        const keepFocus = event => {
            if (event.key !== 'Tab' || viewport.matches || !element.classList.contains('is-open')) return;
            const controls = [trigger, ...element.querySelectorAll('button, a[href]')].filter(item => item?.getClientRects().length);
            const first = controls[0], last = controls.at(-1);
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
            else if (!event.shiftKey && document.activeElement === trigger) { event.preventDefault(); controls[1]?.focus({preventScroll: true}); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
        };
        element.addEventListener('keydown', keepFocus);
        trigger?.addEventListener('keydown', keepFocus);
    }
}
export function focusTrigger(element) { element?.focus({preventScroll: true}); }

export async function startBookingStep(element, step) {
    await new Promise(resolve => requestAnimationFrame(resolve));
    if (!element?.isConnected || element.dataset.bookingStep !== String(step)) return;
    const heading = element.querySelector('#entry-confirmation-title, .booking-content h1, .booking-content h3, #facturaImprimir h4');
    if (heading) {
        heading.setAttribute('tabindex', '-1');
        heading.focus({preventScroll: true});
    }
    window.scrollTo({top: 0, left: 0, behavior: 'instant'});
}
