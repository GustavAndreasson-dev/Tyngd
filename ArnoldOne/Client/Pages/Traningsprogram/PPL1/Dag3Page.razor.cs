using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram.PPL1
{
    /// <summary>
    /// Sida för dag 3 i PPL1-programmet (Legs). Sparar användarens vikter i LocalStorage.
    /// </summary>
    public partial class Dag3Page : ComponentBase
    {
        private Transition transition = Transition.None;

        private Dag3Weights weights = new();

        /// <summary>Tjänst för att läsa och skriva användardata i webbläsarens LocalStorage.</summary>
        [Inject]
        private ILocalStorageService LocalStorage { get; set; } = default!;

        private const string StorageKey = "dag3-weights";

        /// <summary>Läser sparade vikter från LocalStorage vid första renderingen.</summary>
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

        /// <summary>Sparar vikter till LocalStorage när användaren lämnar ett fält.</summary>
        private async Task OnWeightChanged(FocusEventArgs _)
        {
            await LocalStorage.SetItemAsync(StorageKey, weights);
        }

        /// <summary>
        /// Användarens sparade vikter för dag 3.
        /// Serialiseras till och från LocalStorage.
        /// </summary>
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
