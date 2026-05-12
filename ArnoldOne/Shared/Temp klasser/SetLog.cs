namespace ArnoldOne.Shared.Temp_klasser;

public class SetLog
{
    /// <summary>Unikt id för raden.</summary>
    public int SetLogId { get; set; }

    /// <summary>FK till övningsloggen.</summary>
    public int ExerciseLogId { get; set; }

    /// <summary>Setnummer (0-baserat index).</summary>
    public int SetIndex { get; set; }

    /// <summary>Antal utförda reps.</summary>
    public double Reps { get; set; }

    /// <summary>Om setet är markerat som klart.</summary>
    public bool IsDone { get; set; }
}
