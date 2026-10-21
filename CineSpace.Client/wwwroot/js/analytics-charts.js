const charts = new WeakMap();
export function connectChart(root, tooltip, reference) {
    const controller = new AbortController(), options = {signal: controller.signal};
    let active = null, pinned = false, lastWidth = 0;
    const hide = () => { if (tooltip.matches(':popover-open')) tooltip.hidePopover(); active?.removeAttribute('aria-describedby'); active = null; pinned = false; };
    const position = (x, y) => {
        const box = tooltip.getBoundingClientRect();
        tooltip.style.left = `${Math.max(12, Math.min(innerWidth - box.width - 12, x + 14))}px`;
        tooltip.style.top = `${Math.max(12, Math.min(innerHeight - box.height - 12, y - box.height - 12))}px`;
    };
    const show = (mark, x, y) => {
        active?.removeAttribute('aria-describedby'); active = mark;
        tooltip.querySelector('[data-tooltip-heading]').textContent = mark.dataset.tooltipTitle;
        tooltip.querySelector('[data-tooltip-number]').textContent = mark.dataset.tooltipValue;
        tooltip.querySelector('[data-tooltip-description]').textContent = mark.dataset.tooltipDetail || '';
        mark.setAttribute('aria-describedby', tooltip.id);
        if (!tooltip.matches(':popover-open')) tooltip.showPopover();
        position(x, y);
    };
    const atMark = mark => { const box = mark.getBoundingClientRect(); show(mark, box.left + box.width / 2, box.top + box.height / 2); };
    root.addEventListener('pointermove', event => {
        if (event.pointerType === 'touch' || pinned) return;
        const mark = event.target.closest('[data-tooltip-title]');
        if (mark) show(mark, event.clientX, event.clientY); else if (active) hide();
    }, options);
    root.addEventListener('pointerleave', () => { if (!pinned) hide(); }, options);
    root.addEventListener('focusin', event => {
        const mark = event.target.closest('[data-tooltip-title]');
        if (!mark) return;
        atMark(mark);
        // Native focus may scroll an off-screen SVG mark before the next paint.
        requestAnimationFrame(() => { if (mark.isConnected && document.activeElement === mark) atMark(mark); });
    }, options);
    root.addEventListener('focusout', () => { if (!pinned) hide(); }, options);
    root.addEventListener('click', event => {
        const mark = event.target.closest('[data-tooltip-title]');
        if (!mark) return;
        if (pinned && active === mark) hide(); else { atMark(mark); pinned = true; }
    }, options);
    root.addEventListener('keydown', event => {
        const mark = event.target.closest('[data-tooltip-title]');
        if (mark && ['Enter', ' '].includes(event.key)) { event.preventDefault(); mark.dispatchEvent(new MouseEvent('click', {bubbles: true})); }
    }, options);
    document.addEventListener('pointerdown', event => { if (!root.contains(event.target)) hide(); }, options);
    document.addEventListener('keydown', event => { if (event.key === 'Escape') hide(); }, options);
    document.addEventListener('scroll', event => { if (!tooltip.contains(event.target)) hide(); }, {...options, capture: true});
    const resize = new ResizeObserver(() => {
        const width = Math.round(root.clientWidth);
        if (width > 0 && width !== lastWidth) { lastWidth = width; hide(); reference.invokeMethodAsync('AjustarAncho', width).catch(() => {}); }
    });
    resize.observe(root);
    charts.set(root, () => { resize.disconnect(); controller.abort(); hide(); });
}
export function disconnectChart(root) { charts.get(root)?.(); charts.delete(root); }
