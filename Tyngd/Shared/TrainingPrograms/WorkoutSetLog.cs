namespace Tyngd.Shared.TrainingPrograms;

/// <summary>
/// Loggat set för en övning inom en <see cref="WorkoutExerciseLog"/>.
/// Ersätter Reps-dictionaryn i PassData samt CompletedSets i SessionState (LocalStorage/minne).
/// En rad per set per övning per session.
/// </summary>
public class WorkoutSetLog
{
    /// <summary>Unikt id för raden.</summary>
    public int Id { get; set; }

    /// <summary>FK till övningsloggen.</summary>
    public int WorkoutExerciseLogId { get; set; }

    /// <summary>Setnummer (0-baserat index).</summary>
    public int SetIndex { get; set; }

    /// <summary>Antal utförda reps.</summary>
    public double Reps { get; set; }

    /// <summary>Om setet är markerat som klart.</summary>
    public bool IsDone { get; set; }
}
