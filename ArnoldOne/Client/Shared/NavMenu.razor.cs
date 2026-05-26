using ArnoldOne.Client.Services;
using ArnoldOne.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Shared
{
    public partial class NavMenu : ComponentBase, IDisposable
    {
        [Inject]
        private SplitTemplateService SplitTemplateService { get; set; } = default!;

        [Inject]
        private UserSplitsService UserSplitsService { get; set; } = default!;

        [Inject]
        private LoginState LoginState { get; set; } = default!;

        private List<TrainingSplitTemplate> activeSplitTemplates = new();

        protected override async Task OnInitializedAsync()
        {
            LoginState.OnChange += OnLoginStateChanged;
            await LoadActiveSplitsAsync();
        }

        private async void OnLoginStateChanged()
        {
            try
            {
                await LoadActiveSplitsAsync();
                await InvokeAsync(StateHasChanged);
            }
            catch { }
        }

        private async Task LoadActiveSplitsAsync()
        {
            activeSplitTemplates.Clear();

            if (!LoginState.IsLoggedIn)
                return;

            try
            {
                var allTemplates  = await SplitTemplateService.GetAllAsync();
                var activeSplitIds = await UserSplitsService.GetActiveSplitsAsync(LoginState.CurrentUser!.UserId);
                activeSplitTemplates = allTemplates.Where(t => activeSplitIds.Contains(t.SplitId)).ToList();
            }
            catch { }
        }

        public void Dispose() => LoginState.OnChange -= OnLoginStateChanged;
    }
}
