const states = new WeakMap();
let lastFocused = null;
document.addEventListener('focusin', event => {
    if (event.target instanceof HTMLElement && event.target !== document.body
        && !event.target.closest('.global-loading-dialog')) lastFocused = event.target;
}, true);
export function setBusy(dialog, busy) {
    if (!dialog) return;
    let state = states.get(dialog);
    if (busy) {
        if (dialog.open) return;
        // Desabilitar o botão antes da renderização do modal pode transferir o foco ao body.
        const focused = document.activeElement !== document.body ? document.activeElement : lastFocused;
        state = {
            focus: focused,
            scope: focused?.closest('[data-loading-scope]') ?? document,
            overflow: document.body.style.overflow,
            cancel: event => event.preventDefault(),
            keyboard: event => {
                if (event.key === 'Escape' || event.key === 'Tab') {
                    event.preventDefault();
                    dialog.focus();
                }
            },
            focusin: event => {
                if (dialog.open && !dialog.contains(event.target)) dialog.focus();
            }
        };
        states.set(dialog, state);
        dialog.addEventListener('cancel', state.cancel);
        dialog.addEventListener('keydown', state.keyboard);
        document.addEventListener('focusin', state.focusin, true);
        document.body.style.overflow = 'hidden';
        // A top layer nativa torna inertes também portais e diálogos fora do layout.
        dialog.showModal();
        dialog.focus();
    } else {
        if (dialog.open) dialog.close();
        if (!state) return;
        dialog.removeEventListener('cancel', state.cancel);
        dialog.removeEventListener('keydown', state.keyboard);
        document.removeEventListener('focusin', state.focusin, true);
        document.body.style.overflow = state.overflow;
        states.delete(dialog);
        // O escopo é estável mesmo quando a confirmação é substituída pelo resultado.
        const target = state.scope instanceof HTMLElement ? state.scope
            : state.focus?.isConnected && !state.focus.disabled ? state.focus
            : document.querySelector('[data-import-result]');
        if (!target) return;
        const restore = () => {
            if (dialog.open || !target.isConnected) return true;
            if (target.closest('[inert]')) return false;
            target.focus();
            return true;
        };
        // Layout e overlay renderizam separadamente; aguardar a remoção real de inert.
        if (!restore()) {
            const observer = new MutationObserver(() => { if (restore()) observer.disconnect(); });
            observer.observe(document.body, { subtree: true, attributes: true, attributeFilter: ['inert'], childList: true });
        }
    }
}
