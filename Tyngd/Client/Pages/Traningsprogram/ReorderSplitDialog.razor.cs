using Tyngd.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Tyngd.Client.Pages.Traningsprogram;

public partial class ReorderSplitDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public List<CreatePassDto> Passes { get; set; } = new();

    private List<CreatePassDto> orderedPasses = new();
    private CreatePassDto? selectedPass;
    private int? draggedPassIndex;
    private CreatePassDto? draggedExercisePass;
    private int? draggedExerciseIndex;

    protected override void OnInitialized()
    {
        orderedPasses = Passes.Select(ClonePass).ToList();
        selectedPass = orderedPasses.FirstOrDefault();
    }

    private int SelectedPassIndex
    {
        get
        {
            var index = selectedPass is null
                ? -1
                : orderedPasses.IndexOf(selectedPass);
            return index >= 0 ? index : 0;
        }
        set
        {
            if (value >= 0 && value < orderedPasses.Count)
                selectedPass = orderedPasses[value];
        }
    }

    private void BeginPassDrag(int passIndex)
    {
        draggedPassIndex = passIndex;
    }

    private void DropPass(int targetIndex)
    {
        if (draggedPassIndex is not int sourceIndex)
            return;

        MoveItem(orderedPasses, sourceIndex, targetIndex);
        ClearPassDrag();
    }

    private void MovePass(int passIndex, int offset)
    {
        MoveItem(orderedPasses, passIndex, passIndex + offset);
    }

    private void ClearPassDrag()
    {
        draggedPassIndex = null;
    }

    private void BeginExerciseDrag(CreatePassDto pass, int exerciseIndex)
    {
        draggedExercisePass = pass;
        draggedExerciseIndex = exerciseIndex;
    }

    private void DropExercise(CreatePassDto pass, int targetIndex)
    {
        if (!ReferenceEquals(draggedExercisePass, pass)
            || draggedExerciseIndex is not int sourceIndex)
        {
            return;
        }

        MoveItem(pass.Exercises, sourceIndex, targetIndex);
        ClearExerciseDrag();
    }

    private static void MoveExercise(
        CreatePassDto pass,
        int exerciseIndex,
        int offset)
    {
        MoveItem(pass.Exercises, exerciseIndex, exerciseIndex + offset);
    }

    private void ClearExerciseDrag()
    {
        draggedExercisePass = null;
        draggedExerciseIndex = null;
    }

    private void Apply()
    {
        MudDialog.Close(DialogResult.Ok(orderedPasses));
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private static void MoveItem<T>(List<T> items, int sourceIndex, int targetIndex)
    {
        if (sourceIndex < 0
            || sourceIndex >= items.Count
            || targetIndex < 0
            || targetIndex >= items.Count
            || sourceIndex == targetIndex)
        {
            return;
        }

        var item = items[sourceIndex];
        items.RemoveAt(sourceIndex);
        items.Insert(targetIndex, item);
    }

    private static CreatePassDto ClonePass(CreatePassDto pass) =>
        new()
        {
            PassId = pass.PassId,
            PassName = pass.PassName,
            Color = pass.Color,
            Exercises = pass.Exercises.Select(CloneExercise).ToList(),
        };

    private static CreateExerciseDto CloneExercise(CreateExerciseDto exercise) =>
        new()
        {
            ExerciseId = exercise.ExerciseId,
            ExerciseName = exercise.ExerciseName,
            CatalogExerciseId = exercise.CatalogExerciseId,
            DefaultSets = exercise.DefaultSets,
            RepsMin = exercise.RepsMin,
            RepsMax = exercise.RepsMax,
            DefaultWeight = exercise.DefaultWeight,
        };

    private static string PassName(CreatePassDto pass, int index) =>
        string.IsNullOrWhiteSpace(pass.PassName) ? $"Pass {index + 1}" : pass.PassName;

    private static string ExerciseName(CreateExerciseDto exercise, int index) =>
        string.IsNullOrWhiteSpace(exercise.ExerciseName)
            ? $"Övning {index + 1}"
            : exercise.ExerciseName;

}
