namespace ArnoldOne.Shared.DTOs;

public class PassTemplateExerciseCountDto
{
    public int PassTemplateId { get; set; }
    public string PassTemplateName { get; set; } = string.Empty;
    public int SplitTemplateId { get; set; }
    public string SplitTemplateName { get; set; } = string.Empty;
    public int ExerciseCount { get; set; }
}
