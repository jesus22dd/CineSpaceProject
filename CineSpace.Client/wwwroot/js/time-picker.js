const clocks = new WeakMap();
export function connectTimeDial(dial, reference) {
    const abort = new AbortController(); const options = {signal: abort.signal}; let dragging = false, value = 0, suppressClick = false;
    const convert = event => {
        const r = dial.getBoundingClientRect(); const angle = (Math.atan2(event.clientY - r.y - r.height / 2, event.clientX - r.x - r.width / 2) + Math.PI / 2 + Math.PI * 2) % (Math.PI * 2);
        if (dial.dataset.mode === 'minutes') return Math.round(angle / (Math.PI * 2) * 60) % 60;
        const n = Math.round(angle / (Math.PI * 2) * 12) % 12;
        if (dial.dataset.clockTime === 'true') return n === 0 ? 12 : n;
        const distance = Math.hypot(event.clientX - r.x - r.width / 2, event.clientY - r.y - r.height / 2);
        return n === 0 && +dial.dataset.hourMax === 12 && distance < r.width * .33 ? 12 : Math.min(n, +dial.dataset.hourMax);
    };
    const preview = n => {
        value = n; const angle = n * (dial.dataset.mode === 'minutes' ? 6 : 30), rad = angle * Math.PI / 180;
        dial.querySelector('.dial-needle').setAttribute('transform', `rotate(${angle} 120 120)`);
        const marker = dial.querySelector('.time-dial-marker'); marker.style.left = `${50 + 43 * Math.sin(rad)}%`; marker.style.top = dial.dataset.mode === 'hours' && dial.dataset.clockTime !== 'true' && n === 12 ? '24%' : `${50 - 43 * Math.cos(rad)}%`;
        // Preserve Blazor's text node so keyboard updates can still render it.
        if (marker.firstChild) marker.firstChild.nodeValue = dial.dataset.mode === 'minutes' ? String(n).padStart(2, '0') : String(n);
    };
    dial.addEventListener('pointerdown', event => { if (event.button !== 0 || !event.isPrimary) return; event.preventDefault(); dragging = true; suppressClick = true; dial.setPointerCapture(event.pointerId); dial.focus({preventScroll: true}); preview(convert(event)); }, options);
    dial.addEventListener('pointermove', event => { if (dragging) preview(convert(event)); }, options);
    const end = event => { if (!dragging) return; dragging = false; if (dial.hasPointerCapture(event.pointerId)) dial.releasePointerCapture(event.pointerId); reference.invokeMethodAsync('ElegirDesdeReloj', value, true).catch(() => {}); setTimeout(() => { suppressClick = false; }, 0); };
    dial.addEventListener('pointerup', end, options); dial.addEventListener('pointercancel', end, options);
    dial.addEventListener('click', event => { if (suppressClick) { event.preventDefault(); event.stopPropagation(); suppressClick = false; } }, options);
    dial.addEventListener('keydown', event => {
        if (event.key === 'Enter' && event.target === dial) { event.preventDefault(); reference.invokeMethodAsync('ElegirDesdeReloj', +dial.dataset.value, true).catch(() => {}); return; }
        if (!['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(event.key)) return;
        event.preventDefault(); const max = dial.dataset.mode === 'minutes' ? 59 : dial.dataset.clockTime === 'true' ? 12 : +dial.dataset.hourMax, min = dial.dataset.mode === 'hours' && dial.dataset.clockTime === 'true' ? 1 : 0;
        const n = Math.max(min, Math.min(max, +dial.dataset.value + (event.key === 'ArrowUp' || event.key === 'ArrowRight' ? 1 : -1)));
        dial.focus({preventScroll: true}); reference.invokeMethodAsync('ElegirDesdeReloj', n, false).catch(() => {});
    }, options);
    clocks.set(dial, abort);
}
export function disconnectTimeDial(dial) { clocks.get(dial)?.abort(); clocks.delete(dial); }
export async function restoreScheduleFocus(element, field) { await new Promise(requestAnimationFrame); if (element.isConnected) element.querySelector(`[data-time-field="${field}"]`)?.focus({preventScroll: true}); }

const columns = new WeakMap();
export function connectTimeColumns(element, reference) {
    const abort = new AbortController(), options = {signal: abort.signal, capture: true};
    const open = () => element.dataset.timeColumnsOpen === 'true';
    document.addEventListener('click', event => {
        if (open() && !element.contains(event.target)) reference.invokeMethodAsync('CerrarSelectorColumnas', false).catch(() => {});
    }, options);
    let frame;
    const reposition = () => {
        if (!open()) return;
        cancelAnimationFrame(frame); frame = requestAnimationFrame(() => layoutTimeColumns(element));
    };
    window.addEventListener('resize', reposition, options);
    document.addEventListener('scroll', event => { if (!event.target.closest?.('.time-columns-picker')) reposition(); }, options);
    element.addEventListener('keydown', event => {
        if (!open()) return;
        if (event.key === 'Escape') {
            event.preventDefault(); event.stopImmediatePropagation();
            reference.invokeMethodAsync('CerrarSelectorColumnas', true).catch(() => {}); return;
        }
        const option = event.target.closest('[role="option"]'); if (!option) return;
        if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
            event.preventDefault(); event.stopPropagation();
            const lists = [...element.querySelectorAll('.time-column-options')], index = lists.indexOf(option.parentElement);
            const next = lists[index + (event.key === 'ArrowLeft' ? -1 : 1)];
            next?.querySelector('[aria-selected="true"]')?.focus({preventScroll: true}); return;
        }
        if (!['ArrowUp', 'ArrowDown', 'Home', 'End'].includes(event.key)) return;
        event.preventDefault(); event.stopPropagation();
        const choices = [...option.parentElement.querySelectorAll('[role="option"]:not(:disabled)')];
        const index = choices.indexOf(option), next = event.key === 'Home' ? choices[0] : event.key === 'End' ? choices.at(-1) : choices[Math.max(0, Math.min(choices.length - 1, index + (event.key === 'ArrowDown' ? 1 : -1)))];
        if (next) { next.click(); next.focus({preventScroll: true}); next.scrollIntoView({block: 'nearest'}); }
    }, options);
    columns.set(element, abort);
}
export function disconnectTimeColumns(element) { columns.get(element)?.abort(); columns.delete(element); }
function layoutTimeColumns(element) {
    const panel = element.querySelector('.time-columns-picker'), anchor = element.querySelector('.time-entry-control, .time-duration-fields');
    if (!panel || !anchor) return;
    const rect = anchor.getBoundingClientRect(), width = Math.min(rect.width, 328, innerWidth - 24);
    const below = innerHeight - rect.bottom - 20, above = rect.top - 20;
    panel.style.width = `${width}px`;
    panel.style.setProperty('--time-column-height', `${Math.max(88, Math.min(220, Math.max(below, above) - 104))}px`);
    const height = panel.offsetHeight;
    panel.style.left = `${Math.max(12, Math.min(rect.left, innerWidth - width - 12))}px`;
    panel.style.top = `${Math.max(12, Math.min(height <= below || below >= above ? rect.bottom + 8 : rect.top - height - 8, innerHeight - height - 12))}px`;
}
export async function positionTimeColumns(element) {
    await new Promise(requestAnimationFrame);
    const panel = element.querySelector('.time-columns-picker');
    if (!panel) return;
    if (panel.showPopover) { panel.showPopover(); layoutTimeColumns(element); }
    else { panel.style.position = 'relative'; panel.style.width = '100%'; panel.style.marginTop = '12px'; }
    const lists = element.querySelectorAll('.time-column-options');
    for (const list of lists) {
        const selected = list.querySelector('[aria-selected="true"]');
        if (selected) list.scrollTop = selected.offsetTop - (list.clientHeight - selected.offsetHeight) / 2;
    }
    lists[0]?.querySelector('[aria-selected="true"]')?.focus({preventScroll: true});
}
export async function focusTimeColumnsToggle(element) {
    await new Promise(requestAnimationFrame);
    if (element.isConnected) element.querySelector('.time-dropdown-toggle')?.focus({preventScroll: true});
}
