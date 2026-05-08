namespace ArnoldOne.Shared.Temp_klasser;

public class PassTemplate
{
    /// <summary>Unikt id för passmallen.</summary>
    public int PassTemplateId { get; set; }

    /// <summary>FK till splitmallen.</summary>
    public int SplitTemplateId { get; set; }

    /// <summary>Passens namn, t.ex. Push eller Pull.</summary>
    public string PassTemplateName { get; set; } = string.Empty;
}
