namespace Tyngd.App.Presentation;

/// <summary>Ett pass i en split (som /TP/split/{SplitId}/pass/{PassId} i webbappen). Fria pass har också ett SplitId/PassId.</summary>
public record PassArgs(int SplitId, int PassId);

/// <summary>Skapa en ny split (SplitId null) eller redigera en befintlig (som /create-split och /edit-split/{SplitId}).</summary>
public record EditSplitArgs(int? SplitId);

/// <summary>En övning i katalogen (som ExerciseInfoDialog i webbappen).</summary>
public record ExerciseInfoArgs(int CatalogExerciseId);
