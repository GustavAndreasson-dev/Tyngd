using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram
{
    public partial class Dag2Page : ComponentBase
    {
        private Transition transition = Transition.None;

        private Dag2Weights weights = new();

        [Inject]
        private ILocalStorageService LocalStorage { get; set; } = default!;

        private const string StorageKey = "dag2-weights";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var stored = await LocalStorage.GetItemAsync<Dag2Weights>(StorageKey);
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

        private class Dag2Weights
        {
            public decimal? Marklyft { get; set; }
            public decimal? Stangrodd { get; set; }
            public decimal? Latsdrag { get; set; }
            public decimal? Stangcurl { get; set; }
            public decimal? Hammercurls { get; set; }
            public decimal? OmvandaFlyes { get; set; }
        }
    }
}
