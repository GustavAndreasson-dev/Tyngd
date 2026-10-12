namespace Tyngd.Shared.Catalog;

/// <summary>Status för den svenska texten på en katalogövning. Lagras som tinyint.</summary>
public enum CatalogTranslationStatus : byte
{
    /// <summary>Ingen svensk text; engelska visas.</summary>
    Saknas = 0,
    /// <summary>Maskinöversatt, ej granskad.</summary>
    Auto = 1,
    /// <summary>Granskad av admin. Skrivs aldrig över automatiskt.</summary>
    Granskad = 2,
    /// <summary>Granskad, men den engelska källtexten har ändrats sedan dess.</summary>
    BehoverGranskning = 3,
}
