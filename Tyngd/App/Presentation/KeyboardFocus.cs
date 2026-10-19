namespace Tyngd.App.Presentation;

/// <summary>
/// Håller fältet man skriver i synligt när tangentbordet öppnas. Sidan rullar till fältet när det får fokus, men
/// innan tangentbordet krympt sidan (SafeArea SoftInput), så fältet hamnade utanför bild. Rulla igen efter en stund.
/// </summary>
public static class KeyboardFocus
{
    public static void KeepFocusedInputVisible(this UIElement root)
    {
        root.GotFocus += async (_, e) =>
        {
            if (e.OriginalSource is not (TextBox or PasswordBox or AutoSuggestBox) || e.OriginalSource is not UIElement input)
                return;

            await Task.Delay(350);
            if (input.FocusState != FocusState.Unfocused)
                input.StartBringIntoView(new BringIntoViewOptions { VerticalAlignmentRatio = 0.3, AnimationDesired = true });
        };
    }
}
