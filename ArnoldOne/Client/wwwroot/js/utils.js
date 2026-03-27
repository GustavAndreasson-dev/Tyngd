window.arnoldOne = window.arnoldOne || {};

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
