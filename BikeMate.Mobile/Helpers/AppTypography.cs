using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;

namespace BikeMate.Helpers;

public static class AppTypography
{
    public const double CaptionSize = 11;
    public const double BodySize = 13;
    public const double TitleSize = 18;
    private static readonly Color TextDark = Color.FromArgb("#242424");
    private static readonly Color MutedText = Color.FromArgb("#6E6E6E");

    public const string CaptionFont = "PTSansCaption";
    public const string CaptionBoldFont = "PTSansCaptionBold";
    public const string BodyFont = "PublicSans";
    public const string DisplayFont = "Inter";

    public static void ConfigureHandlers(IMauiHandlersCollection handlers)
    {
        LabelHandler.Mapper.AppendToMapping(nameof(AppTypography), (_, view) => Apply(view as Label));
        ButtonHandler.Mapper.AppendToMapping(nameof(AppTypography), (_, view) => Apply(view as Button));
        EntryHandler.Mapper.AppendToMapping(nameof(AppTypography), (_, view) => Apply(view as Entry));
        EditorHandler.Mapper.AppendToMapping(nameof(AppTypography), (_, view) => Apply(view as Editor));
        PickerHandler.Mapper.AppendToMapping(nameof(AppTypography), (_, view) => Apply(view as Picker));
        SearchBarHandler.Mapper.AppendToMapping(nameof(AppTypography), (_, view) => Apply(view as SearchBar));
        DatePickerHandler.Mapper.AppendToMapping(nameof(AppTypography), (_, view) => Apply(view as DatePicker));
        TimePickerHandler.Mapper.AppendToMapping(nameof(AppTypography), (_, view) => Apply(view as TimePicker));
        RadioButtonHandler.Mapper.AppendToMapping(nameof(AppTypography), (_, view) => Apply(view as RadioButton));
    }

    public static double SizeFor(double requestedSize)
    {
        if (requestedSize <= CaptionSize)
        {
            return CaptionSize;
        }

        return requestedSize >= 16 ? TitleSize : BodySize;
    }

    public static string FontFor(double requestedSize, FontAttributes attributes = FontAttributes.None)
    {
        var size = SizeFor(requestedSize);
        if (size <= CaptionSize)
        {
            return (attributes & FontAttributes.Bold) != 0 ? CaptionBoldFont : CaptionFont;
        }

        return size >= TitleSize || (attributes & FontAttributes.Bold) != 0 ? DisplayFont : BodyFont;
    }

    private static void Apply(Label? label)
    {
        if (label is null)
        {
            return;
        }

        ApplyTypography(label.FontSize, label.FontAttributes, out var size, out var font);
        label.FontSize = size;
        label.FontFamily = font;
        label.CharacterSpacing = 0;
    }

    private static void Apply(Button? button)
    {
        if (button is null)
        {
            return;
        }

        ApplyTypography(button.FontSize, button.FontAttributes, out var size, out var font);
        button.FontSize = size;
        button.FontFamily = font;
        button.CharacterSpacing = 0;
    }

    private static void Apply(Entry? entry)
    {
        if (entry is null)
        {
            return;
        }

        ApplyTypography(entry.FontSize, entry.FontAttributes, out var size, out var font);
        entry.FontSize = size;
        entry.FontFamily = font;
        entry.TextColor = TextDark;
        entry.PlaceholderColor = MutedText;
        entry.BackgroundColor = Colors.Transparent;
    }

    private static void Apply(Editor? editor)
    {
        if (editor is null)
        {
            return;
        }

        ApplyTypography(editor.FontSize, editor.FontAttributes, out var size, out var font);
        editor.FontSize = size;
        editor.FontFamily = font;
        editor.TextColor = TextDark;
        editor.PlaceholderColor = MutedText;
        editor.BackgroundColor = Colors.Transparent;
    }

    private static void Apply(Picker? picker)
    {
        if (picker is null)
        {
            return;
        }

        ApplyTypography(picker.FontSize, picker.FontAttributes, out var size, out var font);
        picker.FontSize = size;
        picker.FontFamily = font;
        picker.TextColor = TextDark;
        picker.TitleColor = MutedText;
        picker.BackgroundColor = Colors.Transparent;
    }

    private static void Apply(SearchBar? searchBar)
    {
        if (searchBar is null)
        {
            return;
        }

        ApplyTypography(searchBar.FontSize, searchBar.FontAttributes, out var size, out var font);
        searchBar.FontSize = size;
        searchBar.FontFamily = font;
        searchBar.TextColor = TextDark;
        searchBar.PlaceholderColor = MutedText;
    }

    private static void Apply(DatePicker? datePicker)
    {
        if (datePicker is null)
        {
            return;
        }

        ApplyTypography(datePicker.FontSize, datePicker.FontAttributes, out var size, out var font);
        datePicker.FontSize = size;
        datePicker.FontFamily = font;
        datePicker.TextColor = TextDark;
        datePicker.BackgroundColor = Colors.Transparent;
    }

    private static void Apply(TimePicker? timePicker)
    {
        if (timePicker is null)
        {
            return;
        }

        ApplyTypography(timePicker.FontSize, timePicker.FontAttributes, out var size, out var font);
        timePicker.FontSize = size;
        timePicker.FontFamily = font;
        timePicker.TextColor = TextDark;
        timePicker.BackgroundColor = Colors.Transparent;
    }

    private static void Apply(RadioButton? radioButton)
    {
        if (radioButton is null)
        {
            return;
        }

        ApplyTypography(radioButton.FontSize, radioButton.FontAttributes, out var size, out var font);
        radioButton.FontSize = size;
        radioButton.FontFamily = font;
        radioButton.TextColor = TextDark;
    }

    private static void ApplyTypography(
        double fontSize,
        FontAttributes attributes,
        out double normalizedSize,
        out string normalizedFont)
    {
        normalizedSize = SizeFor(fontSize);
        normalizedFont = FontFor(normalizedSize, attributes);
    }
}
