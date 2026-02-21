using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram
{
    public partial class Dag3Page : ComponentBase
    {
        private Transition transition = Transition.Slide;

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
            public int? Knaeboj { get; set; }
            public int? RumanstMarklyft { get; set; }
            public int? LarCurl { get; set; }
            public int? Benspark { get; set; }
            public int? Vadpress { get; set; }
            public int? Crunches { get; set; }
        }
    }
}
