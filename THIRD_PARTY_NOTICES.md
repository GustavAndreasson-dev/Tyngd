# Third-party notices

The source code of this repository is licensed under the MIT License (see `LICENSE`).
The material below comes from other projects and keeps its own license.

## Exercise data and translations: wger (Creative Commons)

The exercise catalog is imported from the [wger](https://wger.de) REST API (`/api/v2/exerciseinfo/`).
wger's README licenses the wger application code under AGPL-3.0-or-later, which does not apply here because none
of wger's code is used. The exercise text is licensed per entry by its contributors. When this repository was
prepared, the English text of the 922 exercises carried these licenses:

| License | Exercises |
|---|---|
| [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 774 |
| [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) | 128 |
| [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 20 |

The license and the author of each exercise's text are stored in the catalog table (`License`, `LicenseAuthor`)
and shown in the exercise dialog in the app. Re-running the sync refreshes them. The sync reads the license of the
text, not the license of the exercise record, which differs for a handful of entries.

`ArnoldOne/Server/Catalog/wger-sv.json` contains Swedish translations of exercise names and descriptions. They are
adaptations of the wger text and are shared under
[CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/). CC BY-SA 3.0 explicitly allows distributing an
adaptation under a later version of the license with the same license elements, so this covers the 128 entries that
were CC BY-SA 3.0 originally. CC0 text carries no restriction. The translations were machine-generated and are not
reviewed by wger. Check the license of an individual exercise at wger.de before reusing it elsewhere.

## Exercise photos and illustrations: wger contributors (loaded from wger.de)

The photos and illustrations shown next to exercises are **not** stored in this repository. The app loads them from
wger.de at run time and shows the author and license next to each image (`ImageAuthor`, `ImageLicense`). At the time
of writing the images were licensed CC BY-SA 4.0 (291) or CC BY-SA 3.0 (88).

The screenshots in `docs/screenshots` show three of these images, used here under their licenses:

| Exercise | Author | License |
|---|---|---|
| Bench press | Everkinetic | CC BY-SA 3.0 |
| Deadlift | philip | CC BY-SA 4.0 |
| Pull-up | Imobard | CC BY-SA 4.0 |

The screenshots are cropped and compressed screen captures of the app, not changes to the artwork itself.

## Muscle images: Wikimedia Commons (CC BY-SA 3.0)

`ArnoldOne/Client/wwwroot/img/muscles/` contains the body silhouettes and the per-muscle overlays used by the muscle map.
They are copied from wger's `core/static/images/muscles` directory. wger's `SOURCES` file there points to Wikimedia
Commons for the two silhouettes:

- [Muscular system.svg](https://commons.wikimedia.org/wiki/File:Muscular_system.svg) and
  [Muscular system-back.svg](https://commons.wikimedia.org/wiki/File:Muscular_system-back.svg),
  by Termininja, licensed [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/).

wger does not credit the individual muscle overlays (`main/` and `secondary/`) separately. They trace the outlines of
the same artwork, so they are treated as derivatives and kept under CC BY-SA 3.0. If you know a more precise source,
please open an issue.

Changes made: editor metadata and comments were removed, numeric precision was reduced, and a shared `viewBox` was
added so the layers line up. The artwork itself is unchanged.

## Libraries

Used through NuGet and not redistributed here. The license of each package was checked against its NuGet metadata:

| Package | License |
|---|---|
| MudBlazor 9.0.0 | MIT |
| Blazored.LocalStorage 4.5.0 | MIT |
| Entity Framework Core, ASP.NET Core (Authentication.JwtBearer, Components.WebAssembly) | MIT |
| xunit.v3 | Apache-2.0 |

The web font Plus Jakarta Sans (SIL Open Font License 1.1) is loaded from Google Fonts at run time.
