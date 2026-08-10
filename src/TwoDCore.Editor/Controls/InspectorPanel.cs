using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using TwoDCore.Core.Audio;
using TwoDCore.Core.Scene;
using TwoDCore.Editor.ViewModels;

namespace TwoDCore.Editor.Controls;

public sealed class InspectorPanel : ScrollViewer
{
    private readonly EditorSession _session;
    private readonly StackPanel _content = new() { Spacing = 8, Margin = new Thickness(10) };

    public InspectorPanel(EditorSession session)
    {
        _session = session;
        Content = _content;
        _session.SelectionChanged += (_, _) => Rebuild();
        Rebuild();
    }

    private void Rebuild()
    {
        _content.Children.Clear();
        SceneObject? item = _session.SelectedObject;
        if (item == null)
        {
            _content.Children.Add(new TextBlock { Text = "Select an object", Opacity = 0.6 });
            return;
        }

        AddHeader(item.Kind.ToString());
        AddText("Name", item.Name, value => item.Name = value);

        if (item is FolderObject or SoundServiceObject) return;

        if (item is not GlobalSoundObject)
        {
            AddNumber("X", item.Position.X, value => item.Position = new(value, item.Position.Y));
            AddNumber("Y", item.Position.Y, value => item.Position = new(item.Position.X, value));
            AddNumber("Width", item.Size.Width, value => item.Size = new SceneSize(value, item.Size.Height).Clamp(1));
            AddNumber("Height", item.Size.Height, value => item.Size = new SceneSize(item.Size.Width, value).Clamp(1));
        }

        if (item is SoundObjectBase sound)
        {
            AddFile("Audio", sound.AudioFilePath, [new FilePickerFileType("Audio") { Patterns = ["*.wav", "*.mp3", "*.wma", "*.ogg"] }], value => sound.AudioFilePath = value);
            AddSlider("Volume", sound.Volume, 0, 1, value => sound.Volume = value, value => $"{value:P0}");
            if (sound is SpatialSoundObject spatial)
            {
                AddNumber("Full radius", spatial.FullVolumeRadius, value => spatial.FullVolumeRadius = value);
                AddNumber("Radius", spatial.Radius, value => spatial.Radius = value);
                AddEnum("Rolloff", spatial.Rolloff, value => spatial.Rolloff = value);
            }
            return;
        }

        AddSlider("Transparency", item.Transparency, 0, 1, value => item.Transparency = value, value => $"{value:P0}");
        if (item is ShapeObject) AddText("Color", item.ColorHex, value => item.ColorHex = value);
        if (item is ImageObject)
        {
            AddFile("Image", item.TexturePath, [FilePickerFileTypes.ImageAll], value => item.TexturePath = value);
        }
    }

    private void AddHeader(string text) => _content.Children.Add(new TextBlock
    {
        Text = text,
        FontSize = 16,
        FontWeight = Avalonia.Media.FontWeight.SemiBold,
        Margin = new Thickness(0, 0, 0, 6)
    });

    private void AddText(string label, string value, Action<string> setter)
    {
        TextBox editor = new() { Text = value };
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
        TextBox editor = new() { Text = value.ToString("0.##") };
        bool captured = false;
        editor.GotFocus += (_, _) => { if (!captured) { _session.Capture(); captured = true; } };
        editor.LostFocus += (_, _) =>
        {
            if (double.TryParse(editor.Text, out double parsed)) setter(parsed);
            _session.CommitPropertyChange($"Changed {label}");
            captured = false;
        };
        AddRow(label, editor);
    }

    private void AddSlider(string label, double value, double minimum, double maximum, Action<double> setter, Func<double, string> formatter)
    {
        Slider slider = new() { Minimum = minimum, Maximum = maximum, Value = value };
        TextBlock display = new() { Text = formatter(value), Width = 44, VerticalAlignment = VerticalAlignment.Center };
        StackPanel editor = new() { Orientation = Orientation.Horizontal, Spacing = 6 };
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
        ComboBox editor = new() { ItemsSource = Enum.GetValues<T>(), SelectedItem = value };
        editor.SelectionChanged += (_, _) =>
        {
            if (editor.SelectedItem is not T selected) return;
            _session.Capture();
            setter(selected);
            _session.CommitPropertyChange($"Changed {label}");
        };
        AddRow(label, editor);
    }

    private void AddFile(string label, string value, IReadOnlyList<FilePickerFileType> types, Action<string> setter)
    {
        TextBox path = new() { Text = value, IsReadOnly = true };
        Button browse = new() { Content = "…", Width = 34 };
        StackPanel editor = new() { Orientation = Orientation.Horizontal, Spacing = 4 };
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
        Grid row = new() { ColumnDefinitions = new ColumnDefinitions("105,*"), ColumnSpacing = 8 };
        TextBlock caption = new() { Text = label, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.8 };
        Grid.SetColumn(editor, 1);
        row.Children.Add(caption);
        row.Children.Add(editor);
        _content.Children.Add(row);
    }
}
