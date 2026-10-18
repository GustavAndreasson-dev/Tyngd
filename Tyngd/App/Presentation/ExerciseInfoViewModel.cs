namespace Tyngd.App.Presentation;

public partial class ExerciseInfoViewModel : ObservableObject
{
    public ExerciseInfoViewModel(ExerciseInfoArgs args)
    {
        Args = args;
    }

    public ExerciseInfoArgs Args { get; }
}
