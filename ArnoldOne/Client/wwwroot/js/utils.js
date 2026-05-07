window.arnoldOne = window.arnoldOne || {};

// Flashar skärmen vitt två gånger för att signalera att vilotiden är slut.
window.arnoldOne.flashScreen = function () {
    const overlay = document.createElement('div');
    overlay.style.cssText = 'position:fixed;inset:0;background:white;opacity:0;z-index:9999;pointer-events:none;transition:opacity 0.15s ease';
    document.body.appendChild(overlay);

    let count = 0;
    function flash() {
        overlay.style.opacity = '0.85';
        setTimeout(() => {
            overlay.style.opacity = '0';
            setTimeout(() => {
                count++;
                if (count < 2) flash();
                else overlay.remove();
            }, 200);
        }, 200);
    }
    flash();
};

// Kopplar Enter-lyssnare på antecknings- och viktfält.
// Anteckningsfältet: Enter avmarkerar, Shift+Enter radbryter.
// Viktfältet: Enter avmarkerar.
window.arnoldOne.attachNoteKeyHandler = function () {
    document.querySelectorAll('.note-field textarea').forEach(function (t) {
        if (!t._noteHandlerAttached) {
            t._noteHandlerAttached = true;
            t.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    t.blur();
                }
            });
        }
    });

    document.querySelectorAll('.weight-field input').forEach(function (t) {
        if (!t._noteHandlerAttached) {
            t._noteHandlerAttached = true;
            t.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    t.blur();
                    document.body.setAttribute('tabindex', '-1');
                    document.body.focus();
                    document.body.removeAttribute('tabindex');
                }
            });
        }
    });
};
