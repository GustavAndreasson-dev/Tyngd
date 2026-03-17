using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram.PPL1
{
    public partial class Dag4Page : ComponentBase
    {
        private Transition transition = Transition.None;

        private Dag4Weights weights = new();

        [Inject]
        private ILocalStorageService LocalStorage { get; set; } = default!;

        private const string StorageKey = "dag4-weights";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var stored = await LocalStorage.GetItemAsync<Dag4Weights>(StorageKey);
                if (stored is not null)
                {
                    weights = stored;
                }

                StateHasChanged();
            }
        }

        private async Task OnWeightChanged(FocusEventArgs _)
        {
            await LocalStorage.SetItemAsync(StorageKey, weights);
        }

        private class Dag4Weights
        {
            public decimal? Smalbank { get; set; }
            public decimal? StangroddOmvant { get; set; }
            public decimal? TricepExtensionHantel { get; set; }
            public decimal? Skullcrushers { get; set; }
            public decimal? BicepsCable { get; set; }
            public decimal? SittandeBicepCurl { get; set; }
        }
    }
}
