using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram
{
    public partial class Dag3Page : ComponentBase
    {
        private Transition transition = Transition.None;

        private Dag3Weights weights = new();

        [Inject]
        private ILocalStorageService LocalStorage { get; set; } = default!;

        private const string StorageKey = "dag3-weights";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var stored = await LocalStorage.GetItemAsync<Dag3Weights>(StorageKey);
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

        private class Dag3Weights
        {
            public decimal? Knaeboj { get; set; }
            public decimal? RumanstMarklyft { get; set; }
            public decimal? LarCurl { get; set; }
            public decimal? Benspark { get; set; }
            public decimal? Vadpress { get; set; }
            public decimal? Crunches { get; set; }
        }
    }
}
