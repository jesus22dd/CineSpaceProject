const dialogs = new Map();
let nextId = 0;
let bodyLock = null;
const focusableControls = element => [...element.querySelectorAll('a[href], button, input, select, textarea, [tabindex="0"]')]
    .filter(control => !control.matches(':disabled') && !control.closest('[inert]') && control.getClientRects().length);
export function openAuthDialog(element, reference, options = {}) {
    const opener = document.activeElement;
    if (!dialogs.size) bodyLock = {overflow: document.body.style.overflow, padding: document.body.style.paddingRight};
    const scrollbar = innerWidth - document.documentElement.clientWidth;
    const dismiss = () => { if (options.dismissible !== false) reference?.invokeMethodAsync('CerrarDialogo').catch(() => {}); };
    const cancel = event => { event.preventDefault(); dismiss(); };
    const keydown = event => {
        if (event.key !== 'Tab') return;
        const controls = focusableControls(element);
        const first = controls[0], last = controls[controls.length - 1];
        if (!first) { event.preventDefault(); element.focus({preventScroll: true}); return; }
        if (!controls.includes(document.activeElement)) { event.preventDefault(); (event.shiftKey ? last : first).focus(); }
        else if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    };
    const outside = event => {
        const rect = element.getBoundingClientRect();
        return event.target === element && (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom);
    };
    // A click may be retargeted to the dialog after a selection starts in an input.
    // Require a stationary press AND release on the backdrop before dismissing it.
    let press = null, backdropClick = false;
    const pointerdown = event => {
        backdropClick = false;
        press = event.isPrimary && event.button === 0 && outside(event)
            ? {id: event.pointerId, x: event.clientX, y: event.clientY} : null;
    };
    const pointerup = event => {
        backdropClick = !!press && event.pointerId === press.id && outside(event)
            && Math.hypot(event.clientX - press.x, event.clientY - press.y) <= 6;
        press = null;
    };
    const pointercancel = () => { press = null; backdropClick = false; };
    const backdrop = event => {
        if (backdropClick && outside(event)) dismiss();
        backdropClick = false;
    };
    const id = ++nextId;
    dialogs.set(id, {element, opener, cancel, backdrop, keydown, pointerdown, pointerup, pointercancel});
    document.body.style.overflow = 'hidden';
    if (dialogs.size === 1 && scrollbar > 0) document.body.style.paddingRight = `${scrollbar}px`;
    element.addEventListener('cancel', cancel);
    element.addEventListener('keydown', keydown);
    element.addEventListener('click', backdrop);
    element.addEventListener('pointerdown', pointerdown);
    element.addEventListener('pointerup', pointerup);
    element.addEventListener('pointercancel', pointercancel);
    element.showModal();
    element.querySelector('[data-dialog-autofocus], #loginEmail')?.focus({preventScroll: true});
    return id;
}
export function closeAuthDialog(id) {
    const state = dialogs.get(id);
    if (!state) return;
    const element = state.element;
    element.removeEventListener('cancel', state.cancel);
    element.removeEventListener('keydown', state.keydown);
    element.removeEventListener('click', state.backdrop);
    element.removeEventListener('pointerdown', state.pointerdown);
    element.removeEventListener('pointerup', state.pointerup);
    element.removeEventListener('pointercancel', state.pointercancel);
    if (element.open) element.close();
    dialogs.delete(id);
    if (!dialogs.size && bodyLock) {
        document.body.style.overflow = bodyLock.overflow;
        document.body.style.paddingRight = bodyLock.padding;
        bodyLock = null;
    }
    const remainingDialog = [...dialogs.values()].reverse().find(dialog => dialog.element.isConnected && dialog.element.open)?.element;
    const openerAvailable = state.opener?.isConnected && !state.opener.matches(':disabled')
        && (!remainingDialog || remainingDialog.contains(state.opener));
    const focusTarget = openerAvailable ? state.opener : remainingDialog && (focusableControls(remainingDialog)[0] || remainingDialog);
    focusTarget?.focus({preventScroll: true});
}
