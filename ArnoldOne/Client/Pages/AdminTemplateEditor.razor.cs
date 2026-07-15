using ArnoldOne.Client.Services;
using ArnoldOne.Client.Shared;
using ArnoldOne.Shared.DTOs;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Client.Pages;

public partial class AdminTemplateEditor : PageComponentBase
{
    [Inject] private LoginState LoginState { get; set; } = default!;
    [Inject] private AdminService AdminService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Parameter] public int? TemplateId { get; set; }

    private bool IsEditMode => TemplateId.HasValue;
    private UpsertAdminSplitTemplateRequest model = NewModel();
    private string? validationMessage;
    private bool isSaving;

    protected override async Task OnInitializedAsync()
    {
        if (!LoginState.IsLoggedIn || LoginState.CurrentUser?.IsAdmin != true || !TemplateId.HasValue)
            return;

        await TryAsync(async () =>
        {
            var template = await AdminService.GetTemplateAsync(TemplateId.Value)
                ?? throw new InvalidOperationException("Mallen hittades inte.");
            model = new UpsertAdminSplitTemplateRequest
            {
                SplitTemplateName = template.SplitTemplateName,
                IsVisible = template.IsVisible,
                Passes = template.Passes.Select(pass => new UpsertAdminPassTemplateRequest
                {
                    PassTemplateId = pass.PassTemplateId,
                    PassTemplateName = pass.PassTemplateName,
                    IsVisible = pass.IsVisible,
                    Exercises = pass.Exercises.Select(exercise => new UpsertAdminExerciseTemplateRequest
                    {
                        ExerciseTemplateId = exercise.ExerciseTemplateId,
                        ExerciseTemplateName = exercise.ExerciseTemplateName,
                        IsVisible = exercise.IsVisible,
                        DefaultSets = exercise.DefaultSets,
                        DefaultRepsMin = exercise.DefaultRepsMin,
                        DefaultRepsMax = exercise.DefaultRepsMax,
                        DefaultWeight = exercise.DefaultWeight,
                    }).ToList(),
                }).ToList(),
            };
        }, "Kunde inte ladda mallen");
    }

    private void AddPass() => model.Passes.Add(new UpsertAdminPassTemplateRequest
    {
        PassTemplateName = $"Pass {model.Passes.Count + 1}",
        Exercises = [NewExercise()],
    });

    private void RemovePass(int index) => model.Passes.RemoveAt(index);
    private void AddExercise(int passIndex) => model.Passes[passIndex].Exercises.Add(NewExercise());
    private void RemoveExercise(int passIndex, int exerciseIndex) => model.Passes[passIndex].Exercises.RemoveAt(exerciseIndex);

    private async Task SaveAsync()
    {
        validationMessage = Validate();
        if (validationMessage is not null || isSaving)
            return;

        isSaving = true;
        await TryAsync(async () =>
        {
            if (TemplateId.HasValue)
                await AdminService.UpdateTemplateAsync(TemplateId.Value, model);
            else
                await AdminService.CreateTemplateAsync(model);
            Navigation.NavigateTo("/admin");
        }, "Kunde inte spara mallen");
        isSaving = false;
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(model.SplitTemplateName)) return "Ange ett mallnamn.";
        if (model.Passes.Count == 0) return "Lägg till minst ett pass.";
        if (model.Passes.Any(pass => string.IsNullOrWhiteSpace(pass.PassTemplateName))) return "Alla pass måste ha ett namn.";
        if (model.Passes.Any(pass => pass.Exercises.Count == 0)) return "Alla pass måste innehålla minst en övning.";
        if (model.Passes.SelectMany(pass => pass.Exercises).Any(exercise => string.IsNullOrWhiteSpace(exercise.ExerciseTemplateName))) return "Alla övningar måste ha ett namn.";
        if (model.Passes.SelectMany(pass => pass.Exercises).Any(exercise => exercise.DefaultRepsMin > exercise.DefaultRepsMax)) return "Reps min får inte vara större än reps max.";
        return null;
    }

    private static UpsertAdminSplitTemplateRequest NewModel() => new()
    {
        Passes =
        [
            new UpsertAdminPassTemplateRequest
            {
                PassTemplateName = "Pass 1",
                Exercises = [NewExercise()],
            },
        ],
    };

    private static UpsertAdminExerciseTemplateRequest NewExercise() => new() { ExerciseTemplateName = "Ny övning" };
}
