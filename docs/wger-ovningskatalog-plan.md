# Plan: övningskatalog från wger

Mål: användare och admin väljer övningar ur en gemensam katalog med svenska namn och beskrivningar,
hämtad från [wger](https://wger.de) och synkad varje vecka. Användarnas splits, pass och loggar ligger
kvar i vår databas precis som i dag.

## Utgångsläge (mätt 2026-10-10)

- `GET https://wger.de/api/v2/exerciseinfo/?limit=1000` ger hela katalogen i ett anrop, ca 6 MB, utan nyckel.
- 920 övningar. Engelska finns på alla, svenska på 3.
- Licens per övning: CC-BY-SA 4 (767), CC-BY-SA 3 (132), CC0 (21). Upphovsperson finns per övning.
- Beskrivningar är HTML (`<p>`, `<ol>` …), ca 300 000 tecken totalt, median 237. 23 saknar beskrivning.
- 277 har bilder, 701 har utrustning. Kategorier: Legs, Back, Arms, Shoulders, Abs, Chest, Cardio, Calves.
- Nya övningar: ca 300 per år (6/vecka i snitt, 67 senaste 30 dagarna).
- `last_update_global` ändras på nästan alla övningar under ett år, även när texten är samma.
  Därför jämför vi en hash av den engelska texten, inte datumet.

I dag är en övning bara fritext: `Exercise.ExerciseName` (per användares pass) och
`ExerciseTemplate.ExerciseTemplateName` (admin-mallar).

## Beslut

1. **Lokal katalog, inte live-anrop.** wger läses bara av import/synk. Appen läser vår egen tabell.
2. **Katalogen är grundmodellen.** Man väljer övningar ur katalogen. Fritext finns kvar men bara för övningar
   som inte finns i katalogen, och väljaren leder dit först. Tekniskt är kopplingen valfri:
   `Exercise` och `ExerciseTemplate` får en valfri koppling, och namn och standardvärden ligger kvar på raden.
3. **Svenska namn och beskrivningar lagras hos oss** med status: `Auto`, `Granskad`, `BehöverGranskning`, `Saknas`.
   Granskad text skrivs aldrig över automatiskt.
4. **Första översättningen görs nu, utan Claude API**: Claude (med subagenter i omgångar) översätter
   alla 920 och resultatet checkas in som en seed-fil. Claude API kopplas in senare för veckans nya övningar.
5. **Beskrivningar sparas som enkel HTML** (bara `p`, `ol`, `ul`, `li`, `strong`, `em`, `br`), saneras vid import
   och renderas därefter. Ingen markdown-konvertering.
6. **Bilder länkas** till wgers URL:er. Vi kopierar dem inte.
7. **Hela katalogen skickas till klienten** (ca 920 rader, utan beskrivning) och filtreras i webbläsaren.
   Ingen sök-API, inga uppslagstabeller för muskler och utrustning: de lagras som text på katalograden.

## Steg

Varje steg är en egen branch och kan mergas för sig.

### 1. Datamodell (`feature/catalog-schema`)

Ny tabell `ExerciseCatalog`:

| Kolumn | Typ | Not |
|---|---|---|
| `CatalogExerciseId` | int identity, PK | |
| `WgerUuid` | uniqueidentifier, unik | Stabil nyckel mot wger |
| `NameEn`, `DescriptionEn` | nvarchar | Källtext |
| `NameSv`, `DescriptionSv` | nvarchar, null | Vår översättning |
| `TranslationStatus` | tinyint | Auto, Granskad, BehöverGranskning, Saknas |
| `SourceHash` | char(64) | SHA-256 av NameEn + DescriptionEn som översättningen gjordes från |
| `Category`, `Muscles`, `Equipment` | nvarchar | Engelska namn, kommaseparerade; svenska visningsnamn via en liten tabell i koden |
| `ImageUrl` | nvarchar, null | Huvudbilden |
| `License`, `LicenseAuthor` | nvarchar | För attribution |
| `IsActive` | bit | Falsk när övningen försvunnit hos wger |
| `LastSyncedUtc` | datetimeoffset | |

`Exercise` och `ExerciseTemplate` får `CatalogExerciseId int NULL` med FK.
Skript `007_Add_ExerciseCatalog.sql`, samma tabeller i `ArnoldDB`-projektet och i `ArnoldContext`.

### 2. Import och synk (`feature/catalog-sync`)

`WgerCatalogSync` i Server:

1. Hämta alla sidor från `exerciseinfo` (följ `next`, `limit=1000`).
2. Ta den engelska översättningen, sanera HTML, räkna `SourceHash`.
3. Upsert på `WgerUuid`:
   - ny övning: spara, svenska från seed-filen om den finns, annars status `Saknas`
   - hash oförändrad: uppdatera bara metadata (muskler, bild, licens)
   - hash ändrad och status `Auto` eller `Saknas`: ny text, status `Saknas` (senare: översätt via Claude API)
   - hash ändrad och status `Granskad`: behåll svenska, sätt `BehöverGranskning`
4. Övningar som inte längre finns hos wger: `IsActive = false`. Aldrig radera, eftersom pass pekar på dem.
5. Allt i en transaktion. Ett lås hindrar två synkar samtidigt.

Körs från en knapp i admin ("Synka katalog") med en sammanfattning: nya, ändrade, inaktiverade.
Veckovis körning läggs till i steg 8, när vi vet hur appen hostas.

Test: synken mot en inspelad wger-respons (en liten JSON-fil i testprojektet) för de fyra fallen i punkt 3.

### 3. Första översättningen (`feature/catalog-seed-sv`)

- Hämta katalogen, skriv `wger-sv.json`: `{ uuid, sourceHash, nameSv, descriptionSv }` per övning.
- Claude översätter i omgångar om ca 100 övningar (subagenter), med en gemensam ordlista så att
  termer blir konsekventa (t.ex. "Bench press" → "Bänkpress", "Lunge" → "Utfall").
- Etablerade engelska gym-termer behålls (t.ex. "Face pull", "Hip thrust").
- HTML-taggar i beskrivningen behålls oförändrade, bara texten översätts.
- Kontroll efteråt: alla uuid finns, inga tomma namn, taggarna är desamma som i källan.
- Status blir `Auto`. Ni granskar i admin (steg 4).

### 4. Admin: granska katalogen (`feature/catalog-admin`)

Ny sida under admin: lista med filter på status och kategori, redigera svenskt namn och beskrivning
bredvid engelska, knapp "Markera granskad". Endpoints bakom befintliga `AdminOnlyFilter`.

### 5. API till klienten (`feature/catalog-api`)

- `GET /api/catalog`: aktiva övningar utan beskrivning (id, namn sv/en, kategori, muskler, utrustning, bild).
  Cachas med ETag eftersom den bara ändras vid synk.
- `GET /api/catalog/{id}`: en övning med beskrivning, licens och upphovsperson.
- Visningsnamn: `NameSv` om den finns, annars `NameEn`.

### 6. Välja övning i UI (`feature/catalog-picker`)

- `CreateSplit` och `AdminTemplateEditor`: övningsfältet blir en `MudAutocomplete` som söker i svenska
  och engelska namn (och wgers alias). Träffar ur katalogen visas först. Sist i listan finns
  "Lägg till egen övning: …", som ger en övning utan koppling.
- Vid val sätts `CatalogExerciseId` och namnet förifylls. Användaren kan ändra namnet.
- `PassPage`: kopplad övning får en info-knapp som visar beskrivning och bild.
- `ActivateSplit` kopierar `CatalogExerciseId` från mallen till användarens övning.
- DTO:erna `CreateExerciseDto`, `UpsertAdminExerciseTemplateRequest` m.fl. får `CatalogExerciseId`.

### 7. Koppla befintliga övningar (`feature/catalog-backfill`)

Skript som kopplar `ExerciseTemplate` och `Exercise` där namnet exakt matchar `NameSv`, `NameEn`
eller ett alias (skiftlägesokänsligt). Resten lämnas okopplade och listas i admin för manuell koppling.
Mallarna först, eftersom de är få och styr nya splits.

### 8. Veckovis synk och Claude API (senare)

- Översättning via Claude API för status `Saknas` (nya och ändrade övningar), resultat `Auto`.
- Schemaläggning beror på hosting: Azure WebJob/Function med timer, eller en hosted service om
  App Service kör med "Always On". Bestäms när det sätts upp.

### 9. Attribution

- Sidfot eller "Om"-sida: "Övningsdata från wger.de, licens CC-BY-SA".
- Övningens info-vy: licens och upphovsperson.
- Våra översättningar är bearbetningar och får därför samma licens. Valfritt: skicka granskade
  översättningar tillbaka till wger.

## Status 2026-10-11

| Steg | Status |
|---|---|
| 1 Datamodell | Klar: `007_Add_ExerciseCatalog.sql` |
| 2 Import och synk | Klar: admin > Övningskatalog > Synka från wger |
| 3 Första översättningen | Klar: `ArnoldOne/Server/Catalog/wger-sv.json` (920 st, status Auto). Att granska först: `docs/wger-oversattning-granska.md` |
| 4 Admin-granskning | Klar: admin > Övningskatalog |
| 5 API till klienten | Klar, utan ETag (listan laddas en gång per sidladdning) |
| 6 Välja övning i UI | Klar: `ExercisePicker` i Skapa split och mall-editorn, info-knapp på passet |
| 7 Koppla befintliga övningar | Skript klart: `008_Link_Exercises_To_Catalog.sql`, körs efter första synken |
| 8 Veckovis synk + Claude API | Ej påbörjat |
| 9 Attribution | Klar: rad i övningens info-vy och längst ner på Inställningar |

Ordning vid driftsättning: kör 007 → driftsätt koden → synka i admin → kör 008.
Koden läser `CatalogExerciseId` på övningar, så 007 måste köras före driftsättning.

## Risker

| Risk | Hantering |
|---|---|
| wger ändrar API:t | All mappning ligger i `WgerCatalogSync`; synken avbryts utan att ändra något om svaret ser fel ut |
| HTML i beskrivningar kan innehålla skadlig kod | Sanering med tillåtelselista vid import |
| Dålig maskinöversättning | Status `Auto` syns i admin; granskning före lansering av väljaren |
| Övning försvinner hos wger | `IsActive = false`; pass och loggar behåller kopplingen |
| Användare har skrivit samma övning på olika sätt | Kopplingen är valfri; fritext fungerar som förut |

## Beslut från genomgång 2026-10-11

1. Katalogen är grundmodellen; fritext bara för övningar som saknas i katalogen.
2. Alla övningar visas, engelska som reserv. Sökningen fungerar på både svenska och engelska.
3. Synk körs manuellt från admin-vyn. Automatisk synk tas senare.
4. Licensinformationen ska finnas men inte störa: en diskret rad i övningens info-vy och på en "Om"-plats.
