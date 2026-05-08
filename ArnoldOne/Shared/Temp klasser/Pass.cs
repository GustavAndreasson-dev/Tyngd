namespace ArnoldOne.Shared.Temp_klasser;

/// <summary>
/// Ett träningspass kopplat till en användares split. Skapas genom att kopiera en <see cref="TrainingPassTemplate"/>.
/// </summary>
public class Pass
{
    /// <summary>Unikt id för passet.</summary>
    public int PassId { get; set; }

    /// <summary>FK till splitten som passet tillhör.</summary>
    public int SplitId { get; set; }

    /// <summary>Passens namn, t.ex. Push eller Pull.</summary>
    public string PassName { get; set; } = string.Empty;
    ///<summary>Om passet ska vara synlig för användaren eller inte.</summary>
    public bool Visability { get; set; } = false;
}