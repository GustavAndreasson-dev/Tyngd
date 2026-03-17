using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram.PPL1
{
    /// <summary>
    /// Sida för dag 2 i PPL1-programmet (Pull). Sparar användarens vikter i LocalStorage.
    /// </summary>
    public partial class Dag2Page : ComponentBase
    {
        private Transition transition = Transition.None;

        private Dag2Weights weights = new();

        /// <summary>Tjänst för att läsa och skriva användardata i webbläsarens LocalStorage.</summary>
        [Inject]
        private ILocalStorageService LocalStorage { get; set; } = default!;

        private const string StorageKey = "dag2-weights";

        /// <summary>Läser sparade vikter från LocalStorage vid första renderingen.</summary>
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

        /// <summary>Sparar vikter till LocalStorage när användaren lämnar ett fält.</summary>
        private async Task OnWeightChanged(FocusEventArgs _)
        {
            await LocalStorage.SetItemAsync(StorageKey, weights);
        }

        /// <summary>
        /// Användarens sparade vikter för dag 2.
        /// Serialiseras till och från LocalStorage.
        /// </summary>
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
