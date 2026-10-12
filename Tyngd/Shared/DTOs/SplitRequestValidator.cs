namespace Tyngd.Shared.DTOs;

public sealed record SplitValidationError(string Key, string Message);

public static class SplitRequestValidator
{
    public static IReadOnlyList<SplitValidationError> Validate(CreateSplitRequest? request)
    {
        var errors = new List<SplitValidationError>();
        if (request is null)
        {
            errors.Add(new("Request", "Splitdata saknas."));
            return errors;
        }

        ValidateName(request.SplitName, "SplitName", "Splittens namn", errors);

        if (request.Passes is null || request.Passes.Count == 0)
        {
            errors.Add(new("Passes", "Splitten måste innehålla minst ett pass."));
            return errors;
        }

        for (var passIndex = 0; passIndex < request.Passes.Count; passIndex++)
        {
            var pass = request.Passes[passIndex];
            var passKey = $"Passes[{passIndex}]";
            ValidateName(pass.PassName, $"{passKey}.PassName", "Passnamn", errors);

            if (pass.Exercises is null || pass.Exercises.Count == 0)
            {
                errors.Add(new($"{passKey}.Exercises", "Varje pass måste innehålla minst en övning."));
                continue;
            }

            for (var exerciseIndex = 0; exerciseIndex < pass.Exercises.Count; exerciseIndex++)
            {
                var exercise = pass.Exercises[exerciseIndex];
                var exerciseKey = $"{passKey}.Exercises[{exerciseIndex}]";
                ValidateName(exercise.ExerciseName, $"{exerciseKey}.ExerciseName", "Övningsnamn", errors);

                if (exercise.DefaultSets is < 1 or > 20)
                    errors.Add(new($"{exerciseKey}.DefaultSets", "Antal set måste vara mellan 1 och 20."));
                if (exercise.RepsMin is < 1 or > 100)
                    errors.Add(new($"{exerciseKey}.RepsMin", "Minsta antal reps måste vara mellan 1 och 100."));
                if (exercise.RepsMax is < 1 or > 100)
                    errors.Add(new($"{exerciseKey}.RepsMax", "Högsta antal reps måste vara mellan 1 och 100."));
                if (exercise.RepsMin > exercise.RepsMax)
                    errors.Add(new($"{exerciseKey}.RepsMax", "Högsta antal reps måste vara minst lika stort som minsta antal reps."));
                if (exercise.DefaultWeight is < 0 or > 1000)
                    errors.Add(new($"{exerciseKey}.DefaultWeight", "Vikten måste vara mellan 0 och 1000 kg."));
            }
        }

        return errors;
    }

    private static void ValidateName(
        string? value,
        string key,
        string label,
        ICollection<SplitValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(new(key, $"{label} måste anges."));
        else if (value.Trim().Length > 50)
            errors.Add(new(key, $"{label} får innehålla högst 50 tecken."));
    }
}
