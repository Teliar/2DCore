using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using TwoDCore.Core.Audio;
using TwoDCore.Core.Scene;
using TwoDCore.Editor.ViewModels;

namespace TwoDCore.Editor.Controls;

public sealed class InspectorPanel : ScrollViewer
{
    private readonly EditorSession _session;
    private readonly StackPanel _content = new() { Spacing = 0 };

    public InspectorPanel(EditorSession session)
    {
        _session = session;
        Background = new SolidColorBrush(Color.Parse("#18191E"));
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        Content = _content;
        _session.SelectionChanged += (_, _) => Rebuild();
        Rebuild();
    }

    private void Rebuild()
    {
        _content.Children.Clear();
        if (_session.SelectedObjects.Count > 1)
        {
            _content.Children.Add(new TextBlock
            {
                Text = $"{_session.SelectedObjects.Count} objects selected",
                Foreground = new SolidColorBrush(Color.Parse("#BFC3D0")),
                Margin = new Thickness(10, 12)
            });
            return;
        }

        SceneObject? item = _session.SelectedObject;
        if (item == null)
        {
            _content.Children.Add(new TextBlock { Text = "Select an object", Opacity = 0.55, Margin = new Thickness(10, 12) });
            return;
        }
        if (item is SoundServiceObject) return;

        AddCategory("General");
        AddText("Name", item.Name, value => item.Name = value);

        if (item is FolderObject) return;

        if (item is not GlobalSoundObject)
        {
            AddCategory("Transform");
            AddNumber("Position X", item.Position.X, value => item.Position = new(value, item.Position.Y));
            AddNumber("Position Y", item.Position.Y, value => item.Position = new(item.Position.X, value));
            AddNumber("Width", item.Size.Width, value => item.Size = new SceneSize(value, item.Size.Height).Clamp(15));
            AddNumber("Height", item.Size.Height, value => item.Size = new SceneSize(item.Size.Width, value).Clamp(15));
        }

        if (item is SoundObjectBase sound)
        {
            AddCategory("Sound");
            AddFile("Audio File", sound.AudioFilePath, [new FilePickerFileType("Audio Files") { Patterns = ["*.wav", "*.mp3", "*.wma", "*.ogg"] }], value => sound.AudioFilePath = value);
            AddSlider("Volume", sound.Volume, 0, 1, value => sound.Volume = value, value => $"{value:P0}");
            if (sound is SpatialSoundObject spatial)
            {
                AddCategory("Spatial Sound");
                AddNumber("Full Volume Radius", spatial.FullVolumeRadius, value => spatial.FullVolumeRadius = Math.Clamp(value, 0, spatial.Radius));
                AddNumber("Radius", spatial.Radius, value => spatial.Radius = Math.Max(10, value));
                AddEnum("Rolloff", spatial.Rolloff, value => spatial.Rolloff = value);
            }
            return;
        }

        AddCategory("Appearance");
        AddSlider("Transparency", item.Transparency, 0, 1, value => item.Transparency = value, value => $"{value:P0}");
        if (item is ShapeObject) AddColor("Color", item.ColorHex, value => item.ColorHex = value);
        if (item is ImageObject) AddFile("Image", item.TexturePath, [FilePickerFileTypes.ImageAll], value => item.TexturePath = value);
    }

    private void AddCategory(string text) => _content.Children.Add(new Border
    {
        Background = new SolidColorBrush(Color.Parse("#202128")),
        BorderBrush = new SolidColorBrush(Color.Parse("#2A2C34")),
        BorderThickness = new Thickness(0, 1, 0, 1),
        Margin = new Thickness(0, _content.Children.Count == 0 ? 0 : 7, 0, 0),
        Child = new TextBlock
        {
            Text = text,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#AAAFBE")),
            Margin = new Thickness(8, 5)
        }
    });

    private void AddText(string label, string value, Action<string> setter)
    {
        TextBox editor = CreateTextBox(value);
        bool captured = false;
        editor.GotFocus += (_, _) => { if (!captured) { _session.Capture(); captured = true; } };
        editor.LostFocus += (_, _) =>
        {
            setter(editor.Text ?? string.Empty);
            _session.CommitPropertyChange($"Changed {label}");
            captured = false;
        };
        AddRow(label, editor);
    }

    private void AddNumber(string label, double value, Action<double> setter)
    {
        TextBox editor = CreateTextBox(value.ToString("0.##", CultureInfo.InvariantCulture));
        bool captured = false;
        editor.GotFocus += (_, _) => { if (!captured) { _session.Capture(); captured = true; } };
        editor.LostFocus += (_, _) =>
        {
            if (double.TryParse(editor.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)) setter(parsed);
            _session.CommitPropertyChange($"Changed {label}");
            captured = false;
        };
        AddRow(label, editor);
    }

    private void AddSlider(string label, double value, double minimum, double maximum, Action<double> setter, Func<double, string> formatter)
    {
        Slider slider = new() { Minimum = minimum, Maximum = maximum, Value = value, MinWidth = 80, VerticalAlignment = VerticalAlignment.Center };
        TextBlock display = new() { Text = formatter(value), Width = 38, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right };
        Grid editor = new() { ColumnDefinitions = new ColumnDefinitions("*,42") };
        Grid.SetColumn(display, 1);
        editor.Children.Add(slider);
        editor.Children.Add(display);
        bool captured = false;
        slider.PointerPressed += (_, _) => { if (!captured) { _session.Capture(); captured = true; } };
        slider.PropertyChanged += (_, args) =>
        {
            if (args.Property != RangeBase.ValueProperty) return;
            setter(slider.Value);
            display.Text = formatter(slider.Value);
            _session.CommitPropertyChange($"Changed {label}");
        };
        slider.PointerReleased += (_, _) => captured = false;
        AddRow(label, editor);
    }

    private void AddEnum<T>(string label, T value, Action<T> setter) where T : struct, Enum
    {
        ComboBox editor = new() { ItemsSource = Enum.GetValues<T>(), SelectedItem = value, HorizontalAlignment = HorizontalAlignment.Stretch };
        editor.SelectionChanged += (_, _) =>
        {
            if (editor.SelectedItem is not T selected || EqualityComparer<T>.Default.Equals(selected, value)) return;
            _session.Capture();
            setter(selected);
            _session.CommitPropertyChange($"Changed {label}");
        };
        AddRow(label, editor);
    }

    private void AddColor(string label, string value, Action<string> setter)
    {
        TextBox text = CreateTextBox(value);
        Border swatch = new()
        {
            Width = 25,
            Height = 20,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(ParseColor(value)),
            VerticalAlignment = VerticalAlignment.Center
        };
        Button picker = new() { Content = swatch, Width = 35, Padding = new Thickness(3) };
        Grid editor = new() { ColumnDefinitions = new ColumnDefinitions("*,39"), ColumnSpacing = 3 };
        Grid.SetColumn(picker, 1);
        editor.Children.Add(text);
        editor.Children.Add(picker);

        void Apply(string color)
        {
            _session.Capture();
            setter(color);
            text.Text = color;
            swatch.Background = new SolidColorBrush(ParseColor(color));
            _session.CommitPropertyChange($"Changed {label}");
        }

        text.LostFocus += (_, _) =>
        {
            string candidate = text.Text ?? value;
            try { _ = Color.Parse(candidate); Apply(candidate); }
            catch { text.Text = value; }
        };
        picker.Click += (_, _) =>
        {
            ContextMenu menu = new();
            foreach (string color in new[] { "#FFFFFF", "#FF5555", "#50FA7B", "#8BE9FD", "#BD93F9", "#FFB86C", "#282A36", "#000000" })
            {
                MenuItem item = new() { Header = color, Icon = new Border { Width = 14, Height = 14, Background = new SolidColorBrush(ParseColor(color)) } };
                item.Click += (_, _) => Apply(color);
                menu.Items.Add(item);
            }
            menu.Open(picker);
        };
        AddRow(label, editor);
    }

    private void AddFile(string label, string value, IReadOnlyList<FilePickerFileType> types, Action<string> setter)
    {
        TextBox path = CreateTextBox(value);
        path.IsReadOnly = true;
        Button browse = new() { Content = "...", Width = 35, Padding = new Thickness(4, 2) };
        Grid editor = new() { ColumnDefinitions = new ColumnDefinitions("*,39"), ColumnSpacing = 3 };
        Grid.SetColumn(browse, 1);
        editor.Children.Add(path);
        editor.Children.Add(browse);
        browse.Click += async (_, _) =>
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider.CanOpen != true) return;
            IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"Select {label}",
                AllowMultiple = false,
                FileTypeFilter = types
            });
            string? selected = files.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(selected)) return;
            _session.Capture();
            setter(selected);
            path.Text = selected;
            _session.CommitPropertyChange($"Changed {label}");
        };
        AddRow(label, editor);
    }

    private void AddRow(string label, Control editor)
    {
        Grid row = new()
        {
            ColumnDefinitions = new ColumnDefinitions("112,*"),
            ColumnSpacing = 6,
            MinHeight = 28,
            Margin = new Thickness(7, 2)
        };
        TextBlock caption = new() { Text = label, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.Parse("#DCDDDF")) };
        Grid.SetColumn(editor, 1);
        row.Children.Add(caption);
        row.Children.Add(editor);
        _content.Children.Add(row);
    }

    private static TextBox CreateTextBox(string value) => new()
    {
        Text = value,
        MinHeight = 24,
        Padding = new Thickness(5, 2),
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private static Color ParseColor(string value)
    {
        try { return Color.Parse(value); }
        catch { return Colors.White; }
    }
}
