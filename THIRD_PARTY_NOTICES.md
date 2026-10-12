# Third-party notices

The source code of this repository is licensed under the MIT License (see `LICENSE`).
The material below comes from other projects and keeps its own license.

## Exercise data and translations: wger (Creative Commons)

The exercise catalog is imported from the [wger](https://wger.de) REST API
(`/api/v2/exerciseinfo/`). wger's README licenses the application code under AGPL-3.0-or-later, which does
not apply here because none of wger's code is used. The exercise data is licensed per entry by its
contributors. At the time of the import, the 920 exercises carried:

| License | Exercises |
|---|---|
| CC BY-SA 4.0 | 767 |
| CC BY-SA 3.0 | 132 |
| CC0 | 21 |

The license and the author of every exercise are stored in the catalog table (`License`, `LicenseAuthor`)
and shown in the exercise dialog in the app.

`ArnoldOne/Server/Catalog/wger-sv.json` contains Swedish translations of exercise names and descriptions.
They are adaptations of the wger text, so they are shared under the same terms as the original:
[CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/). The translations were machine-generated
and are not reviewed by wger. Please check the license of an individual exercise at wger.de before reusing it.

Exercise photos and illustrations are not part of this repository. They are loaded from wger.de at run time.

## Muscle images: Wikimedia Commons (CC BY-SA 3.0)

`ArnoldOne/Client/wwwroot/img/muscles/` contains the body silhouettes and muscle overlays used by the muscle map.
They are copied from wger's `core/static/images/muscles` directory, whose `SOURCES` file points to
Wikimedia Commons:

- [Muscular system.svg](https://commons.wikimedia.org/wiki/File:Muscular_system.svg) and
  [Muscular system-back.svg](https://commons.wikimedia.org/wiki/File:Muscular_system-back.svg),
  by Termininja, licensed
  [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/).

Changes made: editor metadata and comments were removed, numeric precision was reduced, and a shared `viewBox`
was added so the layers line up. The artwork itself is unchanged. The images remain under CC BY-SA 3.0.

## Libraries

Used through NuGet and not redistributed here: MudBlazor (MIT), Blazored.LocalStorage (MIT),
Entity Framework Core and ASP.NET Core (MIT), xUnit (Apache-2.0).

The web font Plus Jakarta Sans (SIL Open Font License 1.1) is loaded from Google Fonts.
