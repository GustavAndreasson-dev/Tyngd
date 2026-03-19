using ArnoldOne.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Shared
{
    /// <summary>
    /// Navigeringsmenyn som bygger sina länkar dynamiskt från PPL-splitmallen.
    /// </summary>
    public partial class NavMenu : ComponentBase
    {
        private readonly TrainingSplitTemplate pplTemplate = new TrainingSplitTemplateGenerator().CreatePpl();
        private readonly TrainingSplitTemplate ppl1Template = new TrainingSplitTemplateGenerator().CreatePpl1();
    }
}
