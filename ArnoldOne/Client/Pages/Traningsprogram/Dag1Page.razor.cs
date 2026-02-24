using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram
{
    public partial class Dag1Page : ComponentBase
    {
        private Transition transition = Transition.None;

        private Dag1Weights weights = new();

        [Inject]
        private ILocalStorageService LocalStorage { get; set; } = default!;

        private const string StorageKey = "dag1-weights";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var stored = await LocalStorage.GetItemAsync<Dag1Weights>(StorageKey);
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

        private class Dag1Weights
        {
            public decimal? Bankpress { get; set; }
            public decimal? Militarpress { get; set; }
            public decimal? Lutadbänk { get; set; }
            public decimal? Axelpress { get; set; }
            public decimal? TricepsExtension { get; set; }
            public decimal? TricepsPushdown { get; set; }
        }
    }
}
