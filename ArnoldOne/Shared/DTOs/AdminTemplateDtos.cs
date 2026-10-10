namespace ArnoldOne.Shared.DTOs;

public sealed class AdminSplitTemplateDto
{
    public int SplitTemplateId { get; set; }
    public string SplitTemplateName { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
    public List<AdminPassTemplateDto> Passes { get; set; } = new();
}

public sealed class AdminPassTemplateDto
{
    public int PassTemplateId { get; set; }
    public string PassTemplateName { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
    public int Order { get; set; }
    public List<AdminExerciseTemplateDto> Exercises { get; set; } = new();
}

public sealed class AdminExerciseTemplateDto
{
    public int ExerciseTemplateId { get; set; }
    public string ExerciseTemplateName { get; set; } = string.Empty;
    /// <summary>Koppling till övningskatalogen (wger). Null för egen övning.</summary>
    public int? CatalogExerciseId { get; set; }
    public bool IsVisible { get; set; }
    public int Order { get; set; }
    public int DefaultSets { get; set; }
    public int DefaultRepsMin { get; set; }
    public int DefaultRepsMax { get; set; }
    public int DefaultWeight { get; set; }
}

public sealed class UpsertAdminSplitTemplateRequest
{
    public string SplitTemplateName { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public List<UpsertAdminPassTemplateRequest> Passes { get; set; } = new();
}

public sealed class UpsertAdminPassTemplateRequest
{
    public int? PassTemplateId { get; set; }
    public string PassTemplateName { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public List<UpsertAdminExerciseTemplateRequest> Exercises { get; set; } = new();
}

public sealed class UpsertAdminExerciseTemplateRequest
{
    public int? ExerciseTemplateId { get; set; }
    public string ExerciseTemplateName { get; set; } = string.Empty;
    /// <summary>Koppling till övningskatalogen (wger). Null för egen övning.</summary>
    public int? CatalogExerciseId { get; set; }
    public bool IsVisible { get; set; } = true;
    public int DefaultSets { get; set; } = 3;
    public int DefaultRepsMin { get; set; } = 8;
    public int DefaultRepsMax { get; set; } = 12;
    public int DefaultWeight { get; set; }
}
