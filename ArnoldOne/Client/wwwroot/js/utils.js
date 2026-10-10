window.arnoldOne = window.arnoldOne || {};

// Håller en referens till aktiv wake lock för att kunna släppa den senare.
window.arnoldOne._wakeLock = null;

// Begär wake lock för att förhindra att skärmen stängs av under ett pass.
window.arnoldOne.requestWakeLock = async function () {
    if (!('wakeLock' in navigator)) return;
    try {
        window.arnoldOne._wakeLock = await navigator.wakeLock.request('screen');
        // Återaktivera automatiskt om sidan blir synlig igen (t.ex. efter skärmtimeout)
        document.addEventListener('visibilitychange', window.arnoldOne._onVisibilityChange);
    } catch (e) { }
};

// Frigör wake lock när passet är avslutat.
window.arnoldOne.releaseWakeLock = async function () {
    document.removeEventListener('visibilitychange', window.arnoldOne._onVisibilityChange);
    if (window.arnoldOne._wakeLock) {
        await window.arnoldOne._wakeLock.release();
        window.arnoldOne._wakeLock = null;
    }
};

// Återaktiverar wake lock om sidan becomes visible igen (webbläsaren släpper den automatiskt vid bakgrundskörning).
window.arnoldOne._onVisibilityChange = async function () {
    if (document.visibilityState === 'visible' && window.arnoldOne._wakeLock === null) {
        try {
            window.arnoldOne._wakeLock = await navigator.wakeLock.request('screen');
        } catch (e) { }
    }
};

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

// Håller aktiv övningschip synlig när man bläddrar med pilarna (chip-raden scrollar horisontellt).
window.arnoldOne.scrollActiveChip = function () {
    var chip = document.querySelector('.pass-chip--active');
    if (chip) chip.scrollIntoView({ inline: 'center', block: 'nearest', behavior: 'smooth' });
};
