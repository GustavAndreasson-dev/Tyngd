using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram.PPL1
{
    /// <summary>
    /// Sida för dag 4 i PPL1-programmet (Axlar/armar). Sparar användarens vikter i LocalStorage.
    /// </summary>
    public partial class Dag4Page : ComponentBase
    {
        private Transition transition = Transition.None;

        private Dag4Weights weights = new();

        /// <summary>Tjänst för att läsa och skriva användardata i webbläsarens LocalStorage.</summary>
        [Inject]
        private ILocalStorageService LocalStorage { get; set; } = default!;

        private const string StorageKey = "dag4-weights";

        /// <summary>Läser sparade vikter från LocalStorage vid första renderingen.</summary>
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

        /// <summary>Sparar vikter till LocalStorage när användaren lämnar ett fält.</summary>
        private async Task OnWeightChanged(FocusEventArgs _)
        {
            await LocalStorage.SetItemAsync(StorageKey, weights);
        }

        /// <summary>
        /// Användarens sparade vikter för dag 4.
        /// Serialiseras till och från LocalStorage.
        /// </summary>
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
