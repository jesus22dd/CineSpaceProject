const sessions = new Map();
let nextId = 0;
export function open(dialog) {
    if (!dialog?.isConnected || dialog.open) return;
    const cancel = event => event.preventDefault();
    const id = ++nextId;
    sessions.set(id, {dialog, cancel, overflow:document.body.style.overflow, focus:document.activeElement});
    dialog.addEventListener('cancel', cancel);
    document.body.style.overflow = 'hidden';
    dialog.showModal();
    dialog.focus({preventScroll:true});
    return id;
}
export function close(id) {
    const session = sessions.get(id);
    if (!session) return;
    const {dialog} = session;
    dialog.removeEventListener('cancel', session.cancel);
    if (dialog.open) dialog.close();
    document.body.style.overflow = session.overflow;
    if (session.focus?.isConnected && !session.focus.disabled) session.focus.focus({preventScroll:true});
    sessions.delete(id);
}
