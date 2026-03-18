using ArnoldOne.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Shared
{
    /// <summary>
    /// Navigeringsmenyn som bygger sina länkar dynamiskt från PPL-splitmallen.
    /// </summary>
    public partial class NavMenu : ComponentBase
    {
        // PPL-mallen genereras vid uppstart och används för att bygga navmenyn dynamiskt
        private readonly TrainingSplitTemplate pplTemplate = new TrainingSplitTemplateGenerator().CreatePpl();
    }
}
