using ArnoldOne.Client.Services;
using ArnoldOne.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Shared
{
    public partial class NavMenu : ComponentBase, IDisposable
    {
        [Inject]
        private UserSplitsService UserSplitsService { get; set; } = default!;

        [Inject]
        private LoginState LoginState { get; set; } = default!;

        private List<TrainingSplit> activeSplits = new();

        protected override async Task OnInitializedAsync()
        {
            LoginState.OnChange += OnLoginStateChanged;
            UserSplitsService.SplitsChanged += OnSplitsChanged;
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
            activeSplits.Clear();

            if (!LoginState.IsLoggedIn)
                return;

            try
            {
                activeSplits = await UserSplitsService.GetAllActiveSplitsAsync();
            }
            catch { }
        }

        private async Task OnSplitsChanged()
        {
            await LoadActiveSplitsAsync();
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            LoginState.OnChange -= OnLoginStateChanged;
            UserSplitsService.SplitsChanged -= OnSplitsChanged;
        }
    }
}
