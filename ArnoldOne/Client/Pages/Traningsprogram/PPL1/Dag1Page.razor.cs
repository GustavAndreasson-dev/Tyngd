using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram.PPL1
{
    /// <summary>
    /// Sida för dag 1 i PPL1-programmet (Push). Sparar användarens vikter och reps i LocalStorage.
    /// </summary>
    public partial class Dag1Page : ComponentBase
    {
        private Transition transition = Transition.None;

        private Dag1Weights weights = new();

        private int activeIndex;

        /// <summary>Tjänst för att läsa och skriva användardata i webbläsarens LocalStorage.</summary>
        [Inject]
        private ILocalStorageService LocalStorage { get; set; } = default!;

        private const string StorageKey = "dag1-weights";

        /// <summary>Läser sparade vikter från LocalStorage vid första renderingen.</summary>
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

        /// <summary>Sparar vikter till LocalStorage när användaren lämnar ett fält.</summary>
        private async Task OnWeightChanged(FocusEventArgs _)
        {
            await LocalStorage.SetItemAsync(StorageKey, weights);
        }

        /// <summary>
        /// Användarens sparade vikter och reps för dag 1.
        /// Serialiseras till och från LocalStorage.
        /// </summary>
        private class Dag1Weights
        {
            public decimal? Bankpress { get; set; }
            public decimal? Militarpress { get; set; }
            public decimal? Lutadbänk { get; set; }
            public decimal? Axelpress { get; set; }
            public decimal? TricepsExtension { get; set; }
            public decimal? TricepsPushdown { get; set; }
            public double ValueBankpress1 { get; set; } = 8;
            public double ValueBankpress2 { get; set; } = 8;
            public double ValueBankpress3 { get; set; } = 8;
            public double ValueMilitarpress1 { get; set; } = 8;
            public double ValueMilitarpress2 { get; set; } = 8;
            public double ValueMilitarpress3 { get; set; } = 8;
            public double ValueLutadbänk1 { get; set; } = 10;
            public double ValueLutadbänk2 { get; set; } = 10;
            public double ValueLutadbänk3 { get; set; } = 10;
            public double ValueAxelpress1 { get; set; } = 10;
            public double ValueAxelpress2 { get; set; } = 10;
            public double ValueAxelpress3 { get; set; } = 10;
            public double ValueTricepsExtension1 { get; set; } = 10;
            public double ValueTricepsExtension2 { get; set; } = 10;
            public double ValueTricepsExtension3 { get; set; } = 10;
            public double ValueTricepsPushdown1 { get; set; } = 10;
            public double ValueTricepsPushdown2 { get; set; } = 10;
            public double ValueTricepsPushdown3 { get; set; } = 10;
        }
    }
}
