using Microsoft.Maui.Controls;

namespace BikeMate.Helpers;

public static class AppVisualPolish
{
    public static void Apply(View root)
    {
        Polish(root);
    }

    private static void Polish(View view)
    {
        switch (view)
        {
            case Label label:
                label.FontSize = AppTypography.SizeFor(label.FontSize > 0 ? label.FontSize : AppTypography.BodySize);
                label.FontFamily = AppTypography.FontFor(label.FontSize, label.FontAttributes);
                label.CharacterSpacing = 0;
                label.LineBreakMode = label.LineBreakMode == LineBreakMode.NoWrap
                    ? LineBreakMode.NoWrap
                    : LineBreakMode.WordWrap;
                break;

            case Button button:
                var isSymbol = button.Text is "<" or ">" or "+" or "-" or "x" or "X";
                button.FontSize = isSymbol ? AppTypography.TitleSize : Math.Max(button.FontSize, AppTypography.BodySize);
                button.FontFamily = (button.FontAttributes & FontAttributes.Bold) != 0
                    ? AppTypography.DisplayFont
                    : AppTypography.BodyFont;
                button.CharacterSpacing = 0;
                button.CornerRadius = Math.Clamp(button.CornerRadius <= 0 ? 8 : button.CornerRadius, 0, 8);
                button.Padding = button.Padding == default ? new Thickness(16, 0) : button.Padding;
                button.MinimumHeightRequest = isSymbol ? 40 : 46;
                if (button.HeightRequest > 0 && button.HeightRequest < button.MinimumHeightRequest)
                {
                    button.HeightRequest = button.MinimumHeightRequest;
                }
                break;

            case Entry entry:
                entry.FontSize = AppTypography.BodySize;
                entry.FontFamily = AppTypography.BodyFont;
                entry.CharacterSpacing = 0;
                entry.MinimumHeightRequest = 46;
                break;

            case Editor editor:
                editor.FontSize = AppTypography.BodySize;
                editor.FontFamily = AppTypography.BodyFont;
                editor.CharacterSpacing = 0;
                editor.MinimumHeightRequest = Math.Max(editor.MinimumHeightRequest, 72);
                break;

            case Picker picker:
                picker.FontSize = AppTypography.BodySize;
                picker.FontFamily = AppTypography.BodyFont;
                picker.CharacterSpacing = 0;
                picker.MinimumHeightRequest = 46;
                break;

            case DatePicker datePicker:
                datePicker.FontSize = AppTypography.BodySize;
                datePicker.FontFamily = AppTypography.BodyFont;
                datePicker.CharacterSpacing = 0;
                datePicker.MinimumHeightRequest = 46;
                break;

            case TimePicker timePicker:
                timePicker.FontSize = AppTypography.BodySize;
                timePicker.FontFamily = AppTypography.BodyFont;
                timePicker.CharacterSpacing = 0;
                timePicker.MinimumHeightRequest = 46;
                break;

            case SearchBar searchBar:
                searchBar.FontSize = AppTypography.BodySize;
                searchBar.FontFamily = AppTypography.BodyFont;
                searchBar.CharacterSpacing = 0;
                searchBar.MinimumHeightRequest = 46;
                break;

            case RadioButton radioButton:
                radioButton.FontSize = AppTypography.BodySize;
                radioButton.FontFamily = AppTypography.BodyFont;
                radioButton.MinimumHeightRequest = 44;
                break;
        }

        foreach (var child in Children(view))
        {
            Polish(child);
        }
    }

    private static IEnumerable<View> Children(View view)
    {
        switch (view)
        {
            case Layout layout:
                foreach (var child in layout.Children)
                {
                    if (child is View childView)
                    {
                        yield return childView;
                    }
                }
                break;
            case Border { Content: View child }:
                yield return child;
                break;
            case ScrollView { Content: View child }:
                yield return child;
                break;
            case ContentView { Content: View child }:
                yield return child;
                break;
        }
    }
}
