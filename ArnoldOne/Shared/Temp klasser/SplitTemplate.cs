namespace ArnoldOne.Shared.Temp_klasser;

public class SplitTemplate
{
    /// <summary>Unikt id för splitmallen.</summary>
    public int SplitTemplateId { get; set; }

    /// <summary>Splittens namn, t.ex. Push, Pull, Legs.</summary>
    public string SplitTemplateName { get; set; } = string.Empty;

    ///<summary>Om Splitten ska vara synlig för användaren eller inte.</summary>
    public bool Visability { get; set; } = false;
}

