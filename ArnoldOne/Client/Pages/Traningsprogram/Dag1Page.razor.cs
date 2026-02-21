using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram
{
    public partial class Dag1Page : ComponentBase
    {
        private Transition transition = Transition.Slide;

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
            public int? Bankpress { get; set; }
            public int? Militarpress { get; set; }
            public int? Lutadbänk { get; set; }
            public int? Axelpress { get; set; }
            public int? TricepsExtension { get; set; }
            public int? TricepsPushdown { get; set; }
        }
    }
}
