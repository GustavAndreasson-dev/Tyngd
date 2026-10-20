using Microsoft.AspNetCore.Components;

namespace Tyngd.Client.Shared;

/// <summary>
/// Basklass för sidor med inbyggd laddnings- och felhantering.
/// Använd TryAsync() för att wrappa API-anrop.
/// </summary>
public abstract class PageComponentBase : ComponentBase
{
    protected bool IsLoading { get; private set; }
    protected string? ErrorMessage { get; private set; }

    /// <summary>
    /// Kör en async-operation med automatisk laddningsindikator och felhantering.
    /// Vid fel sätts ErrorMessage och visas för användaren.
    /// </summary>
    protected async Task TryAsync(Func<Task> action, string errorPrefix = "Något gick fel")
    {
        IsLoading    = true;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            await action();
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"{errorPrefix}: {Tyngd.Client.Services.HttpErrorText.For(ex)}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{errorPrefix}: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected void ClearError() => ErrorMessage = null;
}
