namespace ArnoldOne.Shared.Temp_klasser;

public class Split
{
    /// <summary>Unikt id för splitten.</summary>
    public int SplitId { get; set; }

    /// <summary>Splittens namn, t.ex. Push, Pull, Legs.</summary>
    public string SplitName { get; set; } = string.Empty;

    /// <summary>FK till användaren (IdentityUser GUID).</summary>
    public string UserId { get; set; } = string.Empty;

    ///<summary>Om Splitten ska vara synlig för användaren eller inte.</summary>
    public bool Visability { get; set; } = false;
}

