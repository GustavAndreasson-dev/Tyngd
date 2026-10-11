# Plan: lösa pass, favoriter, övningshistorik, startsida och vilotimer (2026-10-11)

Varje del är en egen branch och mergas för sig, i den här ordningen.

## 1. Vilotimer som räknar rätt (`feature/rest-timer`)

I dag räknar timern ner med en loop på en sekund i passidan. Webbläsaren saktar ner eller pausar
loopen när appen ligger i bakgrunden, och timern försvinner när man lämnar passidan.

- `RestTimerState` får `EndsAtUtc`. Återstående tid räknas alltid mot klockan.
- Sluttiden sparas i localStorage, så den överlever omladdning och att appen stängs.
- En liten timer syns i layouten på alla sidor medan vilan pågår (tryck = tillbaka till passet).
- När vilan är slut: blink och vibration om appen är synlig; är den dold när tiden går ut
  visas en notis (om användaren har godkänt notiser) när webbläsaren väcker sidan.
- Beslut: ingen Web Push. Larm när appen är helt stängd ingår inte.

## 2. Favoriter och övningsbibliotek (`feature/favorites`)

- Tabell `UserFavoriteExercise (UserId, CatalogExerciseId, CreatedAtUtc)`, migrering 011.
- `GET/PUT/DELETE api/favorites`.
- Ny sida **Övningar** (`/ovningar`): sök på svenska och engelska, filter på kategori, muskel och
  utrustning, stjärna för favorit, tryck för info-rutan.
- Övningsväljaren visar favoriter först.

## 3. Historik per övning över pass och lösa pass (`feature/exercise-history`)

- Historiken knyts till katalogövningen (`CatalogExerciseId`) i stället för till raden i ett visst pass.
  Samma övning i olika pass och lösa pass får därför en gemensam historik.
- `GET api/Session/history/{catalogExerciseId}`: datum, tyngsta vikt, reps per pass.
- Passidans viktgraf använder katalogövningens historik när övningen är kopplad, annars som i dag.
- Info-rutan visar "Din historik" när man är inloggad.
- Okopplade egna övningar behåller historik per pass som i dag.

## 4. Lösa pass (`feature/free-session`)

- Ett löst pass sparas som ett eget pass i en dold split per användare (`Split.IsFreeSessions`,
  migrering 012). Då fungerar sparning, offline, återupptagning, historik och statistik som för
  vanliga pass utan ny sessionmodell.
- Den dolda splitten visas aldrig i listor över splits och kan inte redigeras eller tas bort där.
- Start från översikten: "Starta löst pass" → välj övningar (favoriter först) → passidan.
- Under ett löst pass kan man lägga till fler övningar.
- Startvikt hämtas från övningens senaste historik.

## 5. Startsida för den som inte är inloggad (`feature/guest-home`)

- Vad appen gör: splits och mallar, passläge med vilotimer, historik, muskelfigur, övningsbibliotek.
- En exempelfigur med muskler och några övningar ur katalogen (katalogen blir läsbar utan inloggning).
- Tydliga knappar för att skapa konto och logga in.
