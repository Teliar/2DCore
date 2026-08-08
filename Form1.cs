using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace _2DCore
{

    public class GameObject : ICustomTypeDescriptor
    {
        [Browsable(false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Category("General")]
        [DisplayName("Name")]
        public virtual string Name { get; set; } = "New Object";

        [Browsable(false)]
        public string ObjectType { get; set; } = "Object";

        [Category("Transform")]
        [DisplayName("Position")]
        public Point Position { get; set; } = new Point(0, 0);

        [Category("Transform")]
        [DisplayName("Size")]
        public Size Size { get; set; } = new Size(60, 60);

        private float transparency = 0.0f;

        [Category("Appearance")]
        [DisplayName("Transparency")]
        [Description("Прозрачность объекта (от 0.0 до 1.0).")]
        public float Transparency
        {
            get => transparency;
            set => transparency = Math.Clamp(value, 0.0f, 1.0f);
        }

        [Category("Appearance")]
        [DisplayName("Color")]
        [Description("Цвет заливки объекта.")]
        [Editor(typeof(DarkDropdownColorEditor), typeof(UITypeEditor))]
        public System.Drawing.Color Color { get; set; } = System.Drawing.Color.White;

        [Browsable(false)]
        public string TexturePath { get; set; } = string.Empty;

        [Category("Appearance")]
        [DisplayName("Image")]
        [Description("Выберите изображение для объекта.")]
        [TypeConverter(typeof(ImageConverter))]
        public Image Texture { get; set; } = null!;

        [Browsable(false)]
        public List<GameObject> Children { get; set; } = new List<GameObject>();

        public virtual GameObject Clone()
        {
            return new GameObject
            {
                Id = this.Id,
                Name = this.Name,
                Position = this.Position,
                Size = this.Size,
                transparency = this.transparency,
                Color = this.Color,
                TexturePath = this.TexturePath,
                Texture = this.Texture != null ? new Bitmap(this.Texture) : null!,
                ObjectType = this.ObjectType,
                Children = this.Children.Select(c => c.Clone()).ToList()
            };
        }

        public override string ToString() => Name;

        #region ICustomTypeDescriptor
        public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);
        public string GetClassName() => TypeDescriptor.GetClassName(this, true);
        public string GetComponentName() => TypeDescriptor.GetComponentName(this, true);
        public TypeConverter GetConverter() => TypeDescriptor.GetConverter(this, true);
        public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);
        public PropertyDescriptor GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(this, true);
        public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true)!;
        public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(this, true);
        public EventDescriptorCollection GetEvents(Attribute[]? attributes) => TypeDescriptor.GetEvents(this, attributes, true);
        public PropertyDescriptorCollection GetProperties() => GetProperties(null);

        public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
        {
            if (ObjectType == "SoundService")
            {
                return new PropertyDescriptorCollection(new PropertyDescriptor[0]);
            }

            var properties = TypeDescriptor.GetProperties(this, attributes, true);
            var filtered = new List<PropertyDescriptor>();

            foreach (PropertyDescriptor prop in properties)
            {
                if (ObjectType != "Image" && prop.Name == nameof(Texture))
                {
                    continue;
                }
                filtered.Add(prop);
            }

            return new PropertyDescriptorCollection(filtered.ToArray());
        }

        public object? GetPropertyOwner(PropertyDescriptor? pd) => this;
        #endregion
    }

    public class SoundService : GameObject
    {
        public SoundService()
        {
            Name = "SoundService";
            ObjectType = "SoundService";
        }

        public override GameObject Clone()
        {
            return new SoundService
            {
                Id = this.Id,
                Name = this.Name,
                Position = this.Position,
                Size = this.Size,
                Transparency = this.Transparency,
                Color = this.Color,
                ObjectType = this.ObjectType,
                Children = this.Children.Select(c => c.Clone()).ToList()
            };
        }

        public override string ToString() => "SoundService";
    }

    public class SoundObject : GameObject
    {
        private string filePath = string.Empty;
        private double volume = 1.0;

        public SoundObject()
        {
            Name = "SoundTrigger";
            ObjectType = "SoundTrigger";
            Size = new Size(50, 50);
            Color = System.Drawing.Color.FromArgb(40, 42, 54);
        }

        [Category("Sound")]
        [DisplayName("Audio File")]
        [Description("Выберите аудиофайл (.mp3, .wav, .wma)")]
        [Editor(typeof(AudioFileEditor), typeof(UITypeEditor))]
        public string FilePath
        {
            get => filePath;
            set => filePath = value;
        }

        [Category("Sound")]
        [DisplayName("Volume")]
        [Description("Громкость воспроизведения (от 0.0 до 1.0)")]
        public double Volume
        {
            get => volume;
            set => volume = Math.Clamp(value, 0.0, 1.0);
        }

        public override GameObject Clone()
        {
            return new SoundObject
            {
                Id = this.Id,
                Name = this.Name,
                Position = this.Position,
                Size = this.Size,
                Transparency = this.Transparency,
                Color = this.Color,
                filePath = this.filePath,
                volume = this.volume,
                ObjectType = this.ObjectType,
                Children = this.Children.Select(c => c.Clone()).ToList()
            };
        }
    }

    public class AudioFileEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) => UITypeEditorEditStyle.Modal;

        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider? provider, object? value)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Audio Files (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    return ofd.FileName;
                }
            }
            return value;
        }
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        public override System.Drawing.Color MenuItemSelected => System.Drawing.Color.FromArgb(50, 52, 65);
        public override System.Drawing.Color MenuItemSelectedGradientBegin => System.Drawing.Color.FromArgb(50, 52, 65);
        public override System.Drawing.Color MenuItemSelectedGradientEnd => System.Drawing.Color.FromArgb(50, 52, 65);
        public override System.Drawing.Color MenuItemPressedGradientBegin => System.Drawing.Color.FromArgb(35, 37, 46);
        public override System.Drawing.Color MenuItemPressedGradientEnd => System.Drawing.Color.FromArgb(35, 37, 46);
        public override System.Drawing.Color MenuItemBorder => System.Drawing.Color.FromArgb(70, 75, 90);
        public override System.Drawing.Color MenuBorder => System.Drawing.Color.FromArgb(40, 42, 50);
        public override System.Drawing.Color ToolStripDropDownBackground => System.Drawing.Color.FromArgb(28, 29, 36);
        public override System.Drawing.Color ImageMarginGradientBegin => System.Drawing.Color.FromArgb(28, 29, 36);
        public override System.Drawing.Color ImageMarginGradientMiddle => System.Drawing.Color.FromArgb(28, 29, 36);
        public override System.Drawing.Color ImageMarginGradientEnd => System.Drawing.Color.FromArgb(28, 29, 36);
    }

    #region DTO Models for Serialization
    public class ProjectDataDTO
    {
        [JsonPropertyName("formatVersion")]
        public int FormatVersion { get; set; } = 1;

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = "New Project";

        [JsonPropertyName("startScene")]
        public string StartScene { get; set; } = "scenes/main.2dscene";

        [JsonPropertyName("scenes")]
        public List<string> Scenes { get; set; } = new List<string> { "scenes/main.2dscene" };

        [JsonPropertyName("settings")]
        public ProjectSettingsDTO Settings { get; set; } = new ProjectSettingsDTO();
    }

    public class ProjectSettingsDTO
    {
        [JsonPropertyName("viewportWidth")]
        public int ViewportWidth { get; set; } = 1200;

        [JsonPropertyName("viewportHeight")]
        public int ViewportHeight { get; set; } = 780;

        [JsonPropertyName("backgroundColorHex")]
        public string BackgroundColorHex { get; set; } = "#121317";
    }

    public class SceneDataDTO
    {
        [JsonPropertyName("formatVersion")]
        public int FormatVersion { get; set; } = 1;

        [JsonPropertyName("sceneName")]
        public string SceneName { get; set; } = "MainScene";

        [JsonPropertyName("objects")]
        public List<GameObjectDTO> Objects { get; set; } = new List<GameObjectDTO>();
    }

    public class GameObjectDTO
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("parentId")]
        public Guid? ParentId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "Object";

        [JsonPropertyName("objectType")]
        public string ObjectType { get; set; } = "Object";

        [JsonPropertyName("components")]
        public List<ComponentDTO> Components { get; set; } = new List<ComponentDTO>();

        [JsonPropertyName("children")]
        public List<GameObjectDTO> Children { get; set; } = new List<GameObjectDTO>();
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(TransformComponentDTO), "Transform")]
    [JsonDerivedType(typeof(RenderComponentDTO), "Render")]
    [JsonDerivedType(typeof(SoundComponentDTO), "Sound")]
    public abstract class ComponentDTO
    {
    }

    public class TransformComponentDTO : ComponentDTO
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; } = 60;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 60;

        [JsonPropertyName("transparency")]
        public float Transparency { get; set; } = 0.0f;
    }

    public class RenderComponentDTO : ComponentDTO
    {
        [JsonPropertyName("colorHex")]
        public string ColorHex { get; set; } = "#FFFFFF";

        [JsonPropertyName("texturePath")]
        public string TexturePath { get; set; } = string.Empty;
    }

    public class SoundComponentDTO : ComponentDTO
    {
        [JsonPropertyName("audioFilePath")]
        public string AudioFilePath { get; set; } = string.Empty;

        [JsonPropertyName("volume")]
        public double Volume { get; set; } = 1.0;
    }
    #endregion

    // Обязательное наследование : Form для устранения ошибки CS0115
    public partial class Form1 : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private string? currentProjectPath = null;
        private string currentProjectName = "New Project";
        private bool isModified = false;

        private enum HandleType { None, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }
        public enum LogType { Info, Warning, Error }

        private readonly System.Drawing.Color bgDark = System.Drawing.Color.FromArgb(18, 19, 23);
        private readonly System.Drawing.Color bgPanel = System.Drawing.Color.FromArgb(24, 25, 30);
        private readonly System.Drawing.Color bgHeader = System.Drawing.Color.FromArgb(32, 33, 40);
        private readonly System.Drawing.Color accentBlue = System.Drawing.Color.FromArgb(52, 120, 246);
        private readonly System.Drawing.Color textColor = System.Drawing.Color.FromArgb(220, 222, 230);

        private TreeView explorerTree;
        private ImageList explorerImageList;
        private PropertyGrid propertiesGrid;
        private Panel viewportPanel;
        
        private SplitContainer outerSplit;
        private RichTextBox outputTextBox;

        private Image addIcon;
        private Image engineLogo;
        private Image terminalIcon;
        private Image shapeHandlesIcon;
        private Image folderIcon;
        private Image folderPageIcon;
        private Image soundServiceIcon;
        private Image soundIcon;
        private Image soundTriggerIcon;

        private List<GameObject> sceneObjects = new List<GameObject>();
        private List<GameObject> selectedObjects = new List<GameObject>();
        private List<GameObject> clipboardObjects = new List<GameObject>();

        private Stack<List<GameObject>> undoStack = new Stack<List<GameObject>>();
        private Stack<List<GameObject>> redoStack = new Stack<List<GameObject>>();

        private bool isDragging = false;
        private bool isResizing = false;
        private bool isPanning = false;
        private bool isBoxSelecting = false;

        private PointF boxSelectStart;
        private PointF boxSelectCurrent;

        private HandleType activeHandle = HandleType.None;

        private float zoom = 1.0f;
        private const float MinZoom = 0.1f;
        private const float MaxZoom = 5.0f;
        private PointF cameraOffset = new PointF(0, 0);

        private Point panStartMousePos;
        private PointF panStartOffset;

        private PointF dragStartWorldPos;
        private Dictionary<GameObject, Point> dragStartObjectPositions = new Dictionary<GameObject, Point>();

        private PointF initialMousePos;
        private Point initialObjPos;
        private Size initialObjSize;

        private const float HandleSize = 8f;
        private TreeNode? hoveredNode = null;
        private GameObject? targetParentGameObject = null;

        public Form1()
        {
            this.Size = new Size(1200, 780);
            this.BackColor = bgDark;
            this.ForeColor = textColor;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            this.FormClosing += Form1_FormClosing;

            addIcon = LoadAddIcon();
            engineLogo = LoadCogIcon(); 
            terminalIcon = LoadTerminalIcon();
            shapeHandlesIcon = LoadShapeHandlesIcon();
            folderIcon = LoadFolderIcon();
            folderPageIcon = LoadFolderPageIcon();
            soundServiceIcon = LoadSoundServiceIcon();
            soundIcon = LoadSoundIcon();
            soundTriggerIcon = LoadSoundTriggerIcon();

            try
            {
                using (Bitmap iconBmp = new Bitmap(engineLogo))
                {
                    IntPtr hIcon = iconBmp.GetHicon();
                    this.Icon = Icon.FromHandle(hIcon);
                    DestroyIcon(hIcon);
                }
            }
            catch { }

            explorerImageList = new ImageList();
            explorerImageList.ImageSize = new Size(16, 16);
            explorerImageList.ColorDepth = ColorDepth.Depth32Bit;
            explorerImageList.Images.Add(LoadCameraIcon());   
            explorerImageList.Images.Add(LoadImagesIcon());   
            explorerImageList.Images.Add(shapeHandlesIcon);   
            explorerImageList.Images.Add(folderIcon);         
            explorerImageList.Images.Add(folderPageIcon);     
            explorerImageList.Images.Add(soundServiceIcon);   
            explorerImageList.Images.Add(soundIcon);          

            BuildTopMenu();

            outerSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 4,
                BackColor = System.Drawing.Color.FromArgb(14, 15, 18)
            };
            this.Controls.Add(outerSplit);

            SplitContainer mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 4,
                BackColor = System.Drawing.Color.FromArgb(14, 15, 18)
            };

            SplitContainer leftSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 4,
                BackColor = System.Drawing.Color.FromArgb(14, 15, 18)
            };

            outerSplit.Panel1.Controls.Add(mainSplit);
            mainSplit.Panel1.Controls.Add(leftSplit);

            Panel propContainer = new Panel { Dock = DockStyle.Fill, BackColor = bgPanel };
            propContainer.Controls.Add(CreateHeaderPanel("Inspector Properties"));

            propertiesGrid = new PropertyGrid 
            { 
                Dock = DockStyle.Fill,
                BackColor = bgPanel,
                ViewBackColor = bgPanel,
                ViewForeColor = textColor,
                LineColor = System.Drawing.Color.FromArgb(38, 40, 48),
                HelpVisible = false,
                CategoryForeColor = System.Drawing.Color.FromArgb(170, 175, 190),
                CategorySplitterColor = System.Drawing.Color.FromArgb(42, 44, 52),
                CommandsVisibleIfAvailable = false,
                PropertySort = PropertySort.Categorized,
                SelectedItemWithFocusBackColor = accentBlue
            };
            
            propertiesGrid.Enter += (object? s, EventArgs e) => {
                SaveStateForUndo();
            };

            propertiesGrid.PropertyValueChanged += (object? s, PropertyValueChangedEventArgs e) => {
                RefreshExplorer();
                viewportPanel.Invalidate();
            };

            propContainer.Controls.Add(propertiesGrid);
            propertiesGrid.BringToFront();
            leftSplit.Panel1.Controls.Add(propContainer);

            Panel viewportContainer = new Panel { Dock = DockStyle.Fill, BackColor = bgDark };
            viewportContainer.Controls.Add(CreateHeaderPanel("Viewport 2D"));

            viewportPanel = new Panel { Dock = DockStyle.Fill, BackColor = bgDark };
            
            typeof(Panel).InvokeMember("DoubleBuffered", 
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, 
                null, viewportPanel, new object[] { true });

            viewportPanel.Paint += ViewportPanel_Paint;
            viewportPanel.MouseDown += ViewportPanel_MouseDown;
            viewportPanel.MouseMove += ViewportPanel_MouseMove;
            viewportPanel.MouseUp += ViewportPanel_MouseUp;
            viewportPanel.MouseWheel += ViewportPanel_MouseWheel;
            viewportPanel.MouseEnter += (object? s, EventArgs e) => viewportPanel.Focus();

            viewportContainer.Controls.Add(viewportPanel);
            viewportPanel.BringToFront();
            leftSplit.Panel2.Controls.Add(viewportContainer);

            Panel explorerContainer = new Panel { Dock = DockStyle.Fill, BackColor = bgPanel };
            explorerContainer.Controls.Add(CreateHeaderPanel("Scene Hierarchy"));

            explorerTree = new TreeView 
            { 
                Dock = DockStyle.Fill, 
                ImageList = explorerImageList,
                ShowLines = false,
                ShowPlusMinus = false,
                Indent = 26,
                DrawMode = TreeViewDrawMode.OwnerDrawAll,
                ItemHeight = 24,
                BackColor = bgPanel,
                ForeColor = textColor,
                BorderStyle = BorderStyle.None,
                AllowDrop = true
            };
            explorerTree.AfterSelect += ExplorerTree_AfterSelect;
            explorerTree.DrawNode += ExplorerTree_DrawNode;
            explorerTree.MouseDown += ExplorerTree_MouseDown;
            explorerTree.MouseMove += ExplorerTree_MouseMove;
            explorerTree.MouseLeave += ExplorerTree_MouseLeave;
            explorerTree.ItemDrag += ExplorerTree_ItemDrag;
            explorerTree.DragEnter += ExplorerTree_DragEnter;
            explorerTree.DragOver += ExplorerTree_DragOver;
            explorerTree.DragDrop += ExplorerTree_DragDrop;

            explorerContainer.Controls.Add(explorerTree);
            explorerTree.BringToFront();
            mainSplit.Panel2.Controls.Add(explorerContainer);

            Panel outputContainer = BuildOutputPanel();
            outerSplit.Panel2.Controls.Add(outputContainer);

            leftSplit.SplitterDistance = 220;
            mainSplit.SplitterDistance = 960;
            outerSplit.SplitterDistance = 530;

            this.Shown += (object? s, EventArgs e) => {
                cameraOffset = new PointF(viewportPanel.Width / 2f, viewportPanel.Height / 2f);
                viewportPanel.Invalidate();
            };

            if (!sceneObjects.OfType<SoundService>().Any())
            {
                sceneObjects.Add(new SoundService());
            }

            RefreshExplorer();
            UpdateWindowTitle();
        }

        private string GetUniqueName(string baseName)
        {
            int maxId = 0;
            bool baseExists = false;

            foreach (var obj in GetAllObjectsRecursive(sceneObjects))
            {
                if (obj.Name == baseName)
                {
                    baseExists = true;
                }
                else if (obj.Name.StartsWith(baseName + "_"))
                {
                    string suffix = obj.Name.Substring(baseName.Length + 1);
                    if (int.TryParse(suffix, out int id))
                    {
                        if (id > maxId) maxId = id;
                    }
                }
            }

            if (!baseExists && maxId == 0) return baseName;
            return $"{baseName}_{Math.Max(1, maxId + 1)}";
        }

        private IEnumerable<GameObject> GetAllObjectsRecursive(IEnumerable<GameObject> list)
        {
            foreach (var obj in list)
            {
                yield return obj;
                foreach (var child in GetAllObjectsRecursive(obj.Children))
                {
                    yield return child;
                }
            }
        }

        private void SaveStateForUndo()
        {
            List<GameObject> snapshot = sceneObjects.Select(o => o.Clone()).ToList();
            undoStack.Push(snapshot);
            redoStack.Clear();
            SetModified(true);
        }

        private void Undo()
        {
            if (undoStack.Count == 0) return;

            redoStack.Push(sceneObjects.Select(o => o.Clone()).ToList());
            sceneObjects = undoStack.Pop();

            RestoreSelectionReferences();
            RefreshExplorer();
            UpdatePropertyGrid();
            viewportPanel.Invalidate();
        }

        private void Redo()
        {
            if (redoStack.Count == 0) return;

            undoStack.Push(sceneObjects.Select(o => o.Clone()).ToList());
            sceneObjects = redoStack.Pop();

            RestoreSelectionReferences();
            RefreshExplorer();
            UpdatePropertyGrid();
            viewportPanel.Invalidate();
        }

        private void RestoreSelectionReferences()
        {
            List<string> selectedNames = selectedObjects.Select(s => s.Name).ToList();
            selectedObjects.Clear();
            var allObjs = GetAllObjectsRecursive(sceneObjects).ToList();
            foreach (var name in selectedNames)
            {
                var match = allObjs.FirstOrDefault(o => o.Name == name);
                if (match != null) selectedObjects.Add(match);
            }
        }

        private void BuildTopMenu()
        {
            MenuStrip mainMenu = new MenuStrip
            {
                BackColor = bgHeader,
                ForeColor = System.Drawing.Color.White,
                Renderer = new ToolStripProfessionalRenderer(new DarkColorTable())
            };

            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File") { ForeColor = System.Drawing.Color.White };
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("New Project", null, (s, e) => NewProjectCommand()) { ForeColor = System.Drawing.Color.White, ShortcutKeyDisplayString = "Ctrl+N" });
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("Open Project...", null, (s, e) => OpenProjectCommand()) { ForeColor = System.Drawing.Color.White, ShortcutKeyDisplayString = "Ctrl+O" });
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("Save Project", null, (s, e) => SaveProjectCommand()) { ForeColor = System.Drawing.Color.White, ShortcutKeyDisplayString = "Ctrl+S" });
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("Save Project As...", null, (s, e) => SaveProjectAsCommand()) { ForeColor = System.Drawing.Color.White, ShortcutKeyDisplayString = "Ctrl+Shift+S" });
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("Exit", null, (s, e) => Close()) { ForeColor = System.Drawing.Color.White });

            ToolStripMenuItem editMenu = new ToolStripMenuItem("Edit") { ForeColor = System.Drawing.Color.White };
            
            editMenu.DropDownItems.Add(new ToolStripMenuItem("Undo", null, (s, e) => Undo()) { ForeColor = System.Drawing.Color.White, ShortcutKeyDisplayString = "Ctrl+Z" });
            editMenu.DropDownItems.Add(new ToolStripMenuItem("Redo", null, (s, e) => Redo()) { ForeColor = System.Drawing.Color.White, ShortcutKeyDisplayString = "Ctrl+Shift+Z" });
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add(new ToolStripMenuItem("Copy", null, (s, e) => CopySelected()) { ForeColor = System.Drawing.Color.White, ShortcutKeyDisplayString = "Ctrl+C" });
            editMenu.DropDownItems.Add(new ToolStripMenuItem("Paste", null, (s, e) => PasteSelected()) { ForeColor = System.Drawing.Color.White, ShortcutKeyDisplayString = "Ctrl+V" });
            editMenu.DropDownItems.Add(new ToolStripMenuItem("Duplicate Object", null, (s, e) => DuplicateSelectedObjects()) { ForeColor = System.Drawing.Color.White, ShortcutKeyDisplayString = "Ctrl+D" });
            editMenu.DropDownItems.Add(new ToolStripMenuItem("Delete Object", null, (s, e) => DeleteSelectedObjects()) { ForeColor = System.Drawing.Color.White, ShortcutKeyDisplayString = "Del" });

            ToolStripMenuItem viewMenu = new ToolStripMenuItem("View") { ForeColor = System.Drawing.Color.White };
            viewMenu.DropDownItems.Add(new ToolStripMenuItem("Toggle Output Panel", null, (s, e) => ToggleOutputPanel()) { ForeColor = System.Drawing.Color.White, ShortcutKeyDisplayString = "Ctrl+~" });

            mainMenu.Items.Add(fileMenu);
            mainMenu.Items.Add(editMenu);
            mainMenu.Items.Add(viewMenu);

            this.Controls.Add(mainMenu);
            this.MainMenuStrip = mainMenu;
        }

        private Panel BuildOutputPanel()
        {
            Panel outputContainer = new Panel { Dock = DockStyle.Fill, BackColor = bgPanel };

            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = bgHeader
            };

            PictureBox iconBox = new PictureBox
            {
                Image = terminalIcon,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(16, 16),
                Location = new Point(8, 6)
            };
            header.Controls.Add(iconBox);

            Label titleLabel = new Label
            {
                Text = "OUTPUT",
                ForeColor = System.Drawing.Color.FromArgb(190, 195, 210),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Location = new Point(28, 0),
                Size = new Size(100, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(titleLabel);

            Button closeBtn = new Button
            {
                Text = "✕",
                Dock = DockStyle.Right,
                Width = 28,
                FlatStyle = FlatStyle.Flat,
                ForeColor = System.Drawing.Color.FromArgb(180, 180, 190),
                Cursor = Cursors.Hand
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(232, 17, 35);
            closeBtn.Click += (object? s, EventArgs e) => ToggleOutputPanel(false);
            header.Controls.Add(closeBtn);

            Button clearBtn = new Button
            {
                Text = "Clear",
                Dock = DockStyle.Right,
                Width = 55,
                FlatStyle = FlatStyle.Flat,
                ForeColor = System.Drawing.Color.FromArgb(170, 170, 180),
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            clearBtn.FlatAppearance.BorderSize = 0;
            clearBtn.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(50, 52, 62);
            clearBtn.Click += (object? s, EventArgs e) => outputTextBox.Clear();
            header.Controls.Add(clearBtn);

            outputContainer.Controls.Add(header);

            outputTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(15, 16, 20),
                ForeColor = textColor,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Consolas", 9f),
                Padding = new Padding(5)
            };
            outputContainer.Controls.Add(outputTextBox);
            outputTextBox.BringToFront();

            return outputContainer;
        }

        public void Log(string text, LogType type = LogType.Info)
        {
            if (outputTextBox == null || outputTextBox.IsDisposed) return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            System.Drawing.Color logColor = textColor;

            switch (type)
            {
                case LogType.Info:
                    logColor = System.Drawing.Color.FromArgb(180, 215, 255);
                    break;
                case LogType.Warning:
                    logColor = System.Drawing.Color.FromArgb(255, 200, 80);
                    break;
                case LogType.Error:
                    logColor = System.Drawing.Color.FromArgb(255, 100, 100);
                    break;
            }

            outputTextBox.SelectionStart = outputTextBox.TextLength;
            outputTextBox.SelectionLength = 0;

            outputTextBox.SelectionColor = System.Drawing.Color.Gray;
            outputTextBox.AppendText($"[{timestamp}] ");

            outputTextBox.SelectionColor = logColor;
            outputTextBox.AppendText($"[{type.ToString().ToUpper()}] {text}\n");

            outputTextBox.ScrollToCaret();
        }

        private void ToggleOutputPanel(bool? show = null)
        {
            if (show.HasValue)
                outerSplit.Panel2Collapsed = !show.Value;
            else
                outerSplit.Panel2Collapsed = !outerSplit.Panel2Collapsed;
        }

        private void DeleteSelectedObjects()
        {
            if (selectedObjects.Count == 0) return;

            SaveStateForUndo();

            foreach (var obj in selectedObjects)
            {
                if (obj is SoundService) continue;
                RemoveObjectFromHierarchy(obj);
            }

            selectedObjects.Clear();
            RefreshExplorer();
            UpdatePropertyGrid();
            viewportPanel.Invalidate();
        }

        private bool RemoveObjectFromHierarchy(GameObject target)
        {
            if (sceneObjects.Contains(target))
            {
                sceneObjects.Remove(target);
                return true;
            }
            foreach (var obj in sceneObjects)
            {
                if (RemoveRecursive(obj, target)) return true;
            }
            return false;
        }

        private bool RemoveRecursive(GameObject parent, GameObject target)
        {
            if (parent.Children.Contains(target))
            {
                parent.Children.Remove(target);
                return true;
            }
            foreach (var child in parent.Children)
            {
                if (RemoveRecursive(child, target)) return true;
            }
            return false;
        }

        private void CopySelected()
        {
            if (selectedObjects.Count == 0) return;
            clipboardObjects = selectedObjects.Where(o => !(o is SoundService)).Select(o => o.Clone()).ToList();
        }

        private void PasteSelected()
        {
            if (clipboardObjects.Count == 0) return;

            SaveStateForUndo();

            foreach (var item in clipboardObjects)
            {
                GameObject copy = item.Clone();
                AssignNewGuids(copy);

                string cleanBase = item.Name;
                int underscoreIdx = cleanBase.LastIndexOf('_');
                if (underscoreIdx > 0 && int.TryParse(cleanBase.Substring(underscoreIdx + 1), out _))
                {
                    cleanBase = cleanBase.Substring(0, underscoreIdx);
                }

                copy.Name = GetUniqueName(cleanBase);
                copy.Position = new Point(item.Position.X + 10, item.Position.Y + 10);
                sceneObjects.Add(copy);
            }

            RefreshExplorer();
            UpdatePropertyGrid();
            viewportPanel.Invalidate();
        }

        private void DuplicateSelectedObjects()
        {
            if (selectedObjects.Count == 0) return;

            SaveStateForUndo();

            List<GameObject> newCopies = new List<GameObject>();

            foreach (var original in selectedObjects)
            {
                if (original is SoundService) continue;

                GameObject clone = original.Clone();
                AssignNewGuids(clone);

                string cleanBase = original.Name;
                int underscoreIdx = cleanBase.LastIndexOf('_');
                if (underscoreIdx > 0 && int.TryParse(cleanBase.Substring(underscoreIdx + 1), out _))
                {
                    cleanBase = cleanBase.Substring(0, underscoreIdx);
                }

                clone.Name = GetUniqueName(cleanBase);
                clone.Position = original.Position;
                
                sceneObjects.Add(clone);
                newCopies.Add(clone);
            }

            selectedObjects.Clear();
            selectedObjects.AddRange(newCopies);

            RefreshExplorer();
            UpdatePropertyGrid();
            viewportPanel.Invalidate();
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                if (!propertiesGrid.ContainsFocus)
                {
                    DeleteSelectedObjects();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.S)
            {
                SaveProjectAsCommand();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.S)
            {
                SaveProjectCommand();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.O)
            {
                OpenProjectCommand();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.N)
            {
                NewProjectCommand();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.Z)
            {
                Redo();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                Undo();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelected();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                PasteSelected();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.D)
            {
                DuplicateSelectedObjects();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.Oemtilde)
            {
                ToggleOutputPanel();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add))
            {
                zoom = Math.Min(zoom * 1.15f, MaxZoom);
                viewportPanel.Invalidate();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract))
            {
                zoom = Math.Max(zoom / 1.15f, MinZoom);
                viewportPanel.Invalidate();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private Panel CreateHeaderPanel(string title)
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = bgHeader
            };

            Label label = new Label
            {
                Text = title.ToUpper(),
                ForeColor = System.Drawing.Color.FromArgb(170, 175, 190),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            header.Controls.Add(label);
            return header;
        }

        private Image LoadSoundTriggerIcon()
        {
            if (File.Exists("SoundTrigger.png")) { try { return Image.FromFile("SoundTrigger.png"); } catch { } }
            if (File.Exists("sound_trigger.png")) { try { return Image.FromFile("sound_trigger.png"); } catch { } }
            if (File.Exists("sound.png")) { try { return Image.FromFile("sound.png"); } catch { } }

            Bitmap bmp = new Bitmap(24, 24);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                using (Brush b = new SolidBrush(System.Drawing.Color.White))
                {
                    g.FillRectangle(b, 2, 8, 5, 8);
                    g.FillPolygon(b, new Point[] { new Point(7, 8), new Point(13, 3), new Point(13, 21), new Point(7, 16) });
                    g.DrawArc(new Pen(b, 2f), 13, 6, 8, 12, -90, 180);
                }
            }
            return bmp;
        }

        private Image LoadSoundServiceIcon()
        {
            if (File.Exists("sound_add.png")) { try { return Image.FromFile("sound_add.png"); } catch { } }
            if (File.Exists("sound.png")) { try { return Image.FromFile("sound.png"); } catch { } }

            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                using (Brush b = new SolidBrush(System.Drawing.Color.FromArgb(232, 100, 100)))
                {
                    g.FillRectangle(b, 1, 5, 4, 6);
                    g.FillPolygon(b, new Point[] { new Point(5, 5), new Point(9, 1), new Point(9, 15), new Point(5, 11) });
                    g.DrawArc(new Pen(b, 2f), 9, 3, 6, 10, -90, 180);
                }
            }
            return bmp;
        }

        private Image LoadSoundIcon()
        {
            if (File.Exists("sound.png")) { try { return Image.FromFile("sound.png"); } catch { } }
            if (File.Exists("SoundTrigger.png")) { try { return Image.FromFile("SoundTrigger.png"); } catch { } }

            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                using (Brush b = new SolidBrush(System.Drawing.Color.FromArgb(100, 180, 232)))
                {
                    g.FillRectangle(b, 1, 5, 4, 6);
                    g.FillPolygon(b, new Point[] { new Point(5, 5), new Point(9, 1), new Point(9, 15), new Point(5, 11) });
                    g.DrawArc(new Pen(b, 2f), 9, 3, 6, 10, -90, 180);
                }
            }
            return bmp;
        }

        private Image LoadFolderIcon()
        {
            if (File.Exists("folder.png")) { try { return Image.FromFile("folder.png"); } catch { } }

            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                using (Brush tabBrush = new SolidBrush(System.Drawing.Color.FromArgb(235, 180, 70)))
                using (Brush bodyBrush = new SolidBrush(System.Drawing.Color.FromArgb(245, 200, 90)))
                {
                    g.FillRectangle(tabBrush, 1, 2, 6, 4);
                    g.FillRectangle(bodyBrush, 1, 5, 14, 9);
                }
            }
            return bmp;
        }

        private Image LoadFolderPageIcon()
        {
            if (File.Exists("folder_page.png")) { try { return Image.FromFile("folder_page.png"); } catch { } }

            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                using (Brush pageBrush = new SolidBrush(System.Drawing.Color.FromArgb(220, 235, 255)))
                using (Pen pagePen = new Pen(System.Drawing.Color.FromArgb(52, 120, 246), 1f))
                {
                    g.FillRectangle(pageBrush, 6, 1, 9, 11);
                    g.DrawRectangle(pagePen, 6, 1, 9, 11);
                }
                using (Brush tabBrush = new SolidBrush(System.Drawing.Color.FromArgb(235, 180, 70)))
                using (Brush bodyBrush = new SolidBrush(System.Drawing.Color.FromArgb(245, 200, 90)))
                {
                    g.FillRectangle(tabBrush, 1, 4, 6, 4);
                    g.FillRectangle(bodyBrush, 1, 7, 14, 8);
                }
            }
            return bmp;
        }

        private Image LoadShapeHandlesIcon()
        {
            if (File.Exists("shape_handles.png")) { try { return Image.FromFile("shape_handles.png"); } catch { } }

            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                using (Brush fill = new SolidBrush(System.Drawing.Color.FromArgb(130, 170, 230)))
                using (Pen pen = new Pen(System.Drawing.Color.FromArgb(52, 120, 246), 1f))
                {
                    g.FillRectangle(fill, 3, 3, 10, 10);
                    g.DrawRectangle(pen, 3, 3, 10, 10);
                }
                g.FillEllipse(Brushes.Black, 1, 1, 4, 4);
                g.FillEllipse(Brushes.White, 2, 2, 2, 2);
                g.FillEllipse(Brushes.Black, 11, 1, 4, 4);
                g.FillEllipse(Brushes.White, 12, 2, 2, 2);
                g.FillEllipse(Brushes.Black, 1, 11, 4, 4);
                g.FillEllipse(Brushes.White, 2, 12, 2, 2);
                g.FillEllipse(Brushes.Black, 11, 11, 4, 4);
                g.FillEllipse(Brushes.White, 12, 12, 2, 2);
            }
            return bmp;
        }

        private Image LoadTerminalIcon()
        {
            if (File.Exists("application_osx_terminal.png")) { try { return Image.FromFile("application_osx_terminal.png"); } catch { } }

            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);

                using (Brush frameBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 200, 205)))
                    g.FillRectangle(frameBrush, 0, 0, 16, 16);

                g.FillEllipse(Brushes.Crimson, 2, 2, 3, 3);
                g.FillEllipse(Brushes.Gold, 6, 2, 3, 3);
                g.FillEllipse(Brushes.MediumSeaGreen, 10, 2, 3, 3);

                using (Brush bodyBrush = new SolidBrush(System.Drawing.Color.FromArgb(30, 30, 30)))
                    g.FillRectangle(bodyBrush, 1, 6, 14, 9);

                using (Pen pen = new Pen(System.Drawing.Color.White, 1.5f))
                {
                    g.DrawLines(pen, new Point[] { new Point(3, 8), new Point(6, 10), new Point(3, 12) });
                }
            }
            return bmp;
        }

        private Image LoadCogIcon()
        {
            if (File.Exists("IconCore.png")) { try { return Image.FromFile("IconCore.png"); } catch { } }
            if (File.Exists("cog.png")) { try { return Image.FromFile("cog.png"); } catch { } }

            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                g.FillEllipse(Brushes.Silver, 2, 2, 12, 12);
                g.DrawEllipse(Pens.White, 2, 2, 12, 12);
                g.FillEllipse(Brushes.DarkSlateGray, 5, 5, 6, 6);
            }
            return bmp;
        }

        private Image LoadCameraIcon()
        {
            if (File.Exists("camera.png")) { try { return Image.FromFile("camera.png"); } catch { } }

            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.FillRectangle(Brushes.Gray, 1, 4, 14, 10);
                g.FillRectangle(Brushes.DarkGray, 5, 2, 6, 2);
                g.FillRectangle(Brushes.Gold, 11, 2, 3, 2);
                g.FillEllipse(Brushes.DeepSkyBlue, 4, 5, 8, 8);
            }
            return bmp;
        }

        private Image LoadImagesIcon()
        {
            if (File.Exists("images.png")) { try { return Image.FromFile("images.png"); } catch { } }

            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.FillRectangle(Brushes.RoyalBlue, 4, 1, 10, 10);
                g.FillRectangle(Brushes.DodgerBlue, 1, 4, 10, 10);
                g.FillRectangle(Brushes.White, 3, 6, 6, 6);
            }
            return bmp;
        }

        private Image LoadAddIcon()
        {
            if (File.Exists("add.png")) { try { return Image.FromFile("add.png"); } catch { } }

            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                g.FillEllipse(Brushes.MediumSeaGreen, 1, 1, 14, 14);
                g.FillRectangle(Brushes.White, 7, 4, 2, 8);
                g.FillRectangle(Brushes.White, 4, 7, 8, 2);
            }
            return bmp;
        }

        private bool IsLastChild(TreeNode node)
        {
            if (node.Parent == null) return false;
            return node.Parent.Nodes[node.Parent.Nodes.Count - 1] == node;
        }

        private void ExplorerTree_DrawNode(object? sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool isSelected = (e.State & TreeNodeStates.Selected) != 0;
            System.Drawing.Color bgColor = isSelected ? accentBlue : explorerTree.BackColor;
            System.Drawing.Color fontColor = isSelected ? System.Drawing.Color.White : textColor;

            using (Brush bgBrush = new SolidBrush(bgColor))
            {
                g.FillRectangle(bgBrush, e.Bounds);
            }

            if (e.Node.Parent == null)
            {
                int yCenter = e.Bounds.Top + e.Bounds.Height / 2;

                if (e.Node.Nodes.Count > 0)
                {
                    int arrowX = 8;
                    using (Pen arrowPen = new Pen(System.Drawing.Color.FromArgb(170, 175, 190), 1.5f))
                    {
                        if (e.Node.IsExpanded)
                        {
                            g.DrawLines(arrowPen, new Point[] {
                                new Point(arrowX, yCenter - 2),
                                new Point(arrowX + 5, yCenter + 3),
                                new Point(arrowX + 10, yCenter - 2)
                            });
                        }
                        else
                        {
                            g.DrawLines(arrowPen, new Point[] {
                                new Point(arrowX + 3, yCenter - 5),
                                new Point(arrowX + 8, yCenter),
                                new Point(arrowX + 3, yCenter + 5)
                            });
                        }
                    }
                }

                int iconIndex = e.Node.ImageIndex;
                if (iconIndex >= 0 && iconIndex < explorerImageList.Images.Count)
                {
                    Image img = explorerImageList.Images[iconIndex];
                    g.DrawImage(img, 24, e.Bounds.Top + (e.Bounds.Height - 16) / 2, 16, 16);
                }

                TextRenderer.DrawText(g, e.Node.Text, explorerTree.Font, 
                    new Point(46, e.Bounds.Top + (e.Bounds.Height - explorerTree.Font.Height) / 2), fontColor);

                int textWidth = TextRenderer.MeasureText(e.Node.Text, explorerTree.Font).Width;
                if (e.Node == hoveredNode)
                {
                    int plusX = 46 + textWidth + 10;
                    int plusY = e.Bounds.Top + (e.Bounds.Height - 16) / 2;
                    g.DrawImage(addIcon, plusX, plusY, 16, 16);
                }
            }
            else
            {
                int yCenter = e.Node.Bounds.Top + e.Node.Bounds.Height / 2;

                using (Pen linePen = new Pen(System.Drawing.Color.FromArgb(70, 75, 90), 1f))
                {
                    linePen.DashStyle = DashStyle.Dot;

                    int parentLineX = e.Node.Parent.Bounds.Left - 10;
                    int childStartX = e.Node.Bounds.Left - 18;

                    g.DrawLine(linePen, parentLineX, yCenter, childStartX, yCenter);

                    int topY = e.Node.Bounds.Top;
                    int bottomY = IsLastChild(e.Node) ? yCenter : e.Node.Bounds.Bottom;
                    g.DrawLine(linePen, parentLineX, topY, parentLineX, bottomY);
                }

                if (e.Node.Nodes.Count > 0)
                {
                    int arrowX = e.Node.Bounds.Left - 24;
                    using (Pen arrowPen = new Pen(System.Drawing.Color.FromArgb(170, 175, 190), 1.5f))
                    {
                        if (e.Node.IsExpanded)
                        {
                            g.DrawLines(arrowPen, new Point[] {
                                new Point(arrowX, yCenter - 2),
                                new Point(arrowX + 5, yCenter + 3),
                                new Point(arrowX + 10, yCenter - 2)
                            });
                        }
                        else
                        {
                            g.DrawLines(arrowPen, new Point[] {
                                new Point(arrowX + 3, yCenter - 5),
                                new Point(arrowX + 8, yCenter),
                                new Point(arrowX + 3, yCenter + 5)
                            });
                        }
                    }
                }

                int iconIndex = e.Node.ImageIndex;
                if (iconIndex >= 0 && iconIndex < explorerImageList.Images.Count)
                {
                    Image img = explorerImageList.Images[iconIndex];
                    int iconX = (e.Node.Nodes.Count > 0) ? (e.Node.Bounds.Left - 10) : (e.Node.Bounds.Left - 18);
                    g.DrawImage(img, iconX, e.Node.Bounds.Top + (e.Node.Bounds.Height - 16) / 2, 16, 16);
                }

                int textX = e.Node.Bounds.Left + 8;
                TextRenderer.DrawText(g, e.Node.Text, explorerTree.Font, 
                    new Point(textX, e.Node.Bounds.Top + 3), fontColor);

                if (e.Node == hoveredNode)
                {
                    int textWidth = TextRenderer.MeasureText(e.Node.Text, explorerTree.Font).Width;
                    int plusX = textX + textWidth + 8;
                    int plusY = e.Node.Bounds.Top + (e.Node.Bounds.Height - 16) / 2;
                    g.DrawImage(addIcon, plusX, plusY, 16, 16);
                }
            }
        }

        private void ExplorerTree_MouseMove(object? sender, MouseEventArgs e)
        {
            TreeNode? node = explorerTree.GetNodeAt(e.Location);
            if (hoveredNode != node)
            {
                hoveredNode = node;
                explorerTree.Invalidate();
            }
        }

        private void ExplorerTree_MouseLeave(object? sender, EventArgs e)
        {
            if (hoveredNode != null)
            {
                hoveredNode = null;
                explorerTree.Invalidate();
            }
        }

        private ContextMenuStrip CreateAddContextMenu(GameObject? parent)
        {
            ContextMenuStrip menu = new ContextMenuStrip
            {
                Renderer = new ToolStripProfessionalRenderer(new DarkColorTable()),
                BackColor = System.Drawing.Color.FromArgb(28, 29, 36),
                ForeColor = System.Drawing.Color.White,
                ShowImageMargin = true
            };

            bool isSoundServiceChild = (parent is SoundService);

            if (isSoundServiceChild)
            {
                ToolStripMenuItem menuSoundItem = new ToolStripMenuItem("Sound", soundIcon) { ForeColor = System.Drawing.Color.White };
                menuSoundItem.Click += (s, e) => AddNewObject("Sound");
                menu.Items.Add(menuSoundItem);
            }
            else
            {
                ToolStripMenuItem menuFolderItem = new ToolStripMenuItem("Folder", folderIcon) { ForeColor = System.Drawing.Color.White };
                menuFolderItem.Click += (s, e) => AddNewObject("Folder");

                ToolStripMenuItem menuImageItem = new ToolStripMenuItem("Image", LoadImagesIcon()) { ForeColor = System.Drawing.Color.White };
                menuImageItem.Click += (s, e) => AddNewObject("Image");

                ToolStripMenuItem menuObjectItem = new ToolStripMenuItem("Object", shapeHandlesIcon) { ForeColor = System.Drawing.Color.White };
                menuObjectItem.Click += (s, e) => AddNewObject("Object");

                ToolStripMenuItem menuSoundTriggerItem = new ToolStripMenuItem("SoundTrigger", soundIcon) { ForeColor = System.Drawing.Color.White };
                menuSoundTriggerItem.Click += (s, e) => AddNewObject("SoundTrigger");

                menu.Items.Add(menuFolderItem);
                menu.Items.Add(menuImageItem);
                menu.Items.Add(menuObjectItem);
                menu.Items.Add(menuSoundTriggerItem);
            }

            return menu;
        }

        private void ExplorerTree_MouseDown(object? sender, MouseEventArgs e)
        {
            TreeNode? node = explorerTree.GetNodeAt(e.Location);
            if (node != null)
            {
                int textWidth = TextRenderer.MeasureText(node.Text, explorerTree.Font).Width;
                int textX = (node.Parent == null) ? 46 : (node.Bounds.Left + 8);
                int plusX = textX + textWidth + 8;
                int plusY = node.Bounds.Top + (node.Bounds.Height - 16) / 2;
                Rectangle plusBounds = new Rectangle(plusX - 2, plusY - 2, 20, 20);

                if (plusBounds.Contains(e.Location))
                {
                    targetParentGameObject = node.Tag as GameObject;
                    ContextMenuStrip dynamicMenu = CreateAddContextMenu(targetParentGameObject);
                    dynamicMenu.Show(explorerTree, new Point(plusX, plusY + 18));
                    return;
                }

                if (e.Location.X < node.Bounds.Left)
                {
                    node.Toggle();
                    return;
                }
            }
        }

        private void ExplorerTree_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node && node.Tag is GameObject)
            {
                DoDragDrop(node, DragDropEffects.Move);
            }
        }

        private void ExplorerTree_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(typeof(TreeNode)))
                e.Effect = DragDropEffects.Move;
            else
                e.Effect = DragDropEffects.None;
        }

        private void ExplorerTree_DragOver(object? sender, DragEventArgs e)
        {
            Point targetPoint = explorerTree.PointToClient(new Point(e.X, e.Y));
            TreeNode? targetNode = explorerTree.GetNodeAt(targetPoint);
            if (targetNode != null)
            {
                explorerTree.SelectedNode = targetNode;
            }
        }

        private void ExplorerTree_DragDrop(object? sender, DragEventArgs e)
        {
            Point targetPoint = explorerTree.PointToClient(new Point(e.X, e.Y));
            TreeNode? targetNode = explorerTree.GetNodeAt(targetPoint);
            TreeNode? draggedNode = e.Data?.GetData(typeof(TreeNode)) as TreeNode;

            if (draggedNode != null && draggedNode.Tag is GameObject draggedObj)
            {
                if (draggedObj is SoundService) return;

                SaveStateForUndo();
                RemoveObjectFromHierarchy(draggedObj);

                if (targetNode == null || targetNode.Tag == null)
                {
                    sceneObjects.Add(draggedObj);
                    if (draggedObj is SoundObject && draggedObj.ObjectType == "Sound")
                    {
                        draggedObj.ObjectType = "SoundTrigger";
                        draggedObj.Name = GetUniqueName("SoundTrigger");
                    }
                }
                else if (targetNode.Tag is SoundService targetService)
                {
                    targetService.Children.Add(draggedObj);
                    if (draggedObj is SoundObject && draggedObj.ObjectType == "SoundTrigger")
                    {
                        draggedObj.ObjectType = "Sound";
                        draggedObj.Name = GetUniqueName("Sound");
                    }
                }
                else if (targetNode.Tag is GameObject targetObj)
                {
                    if (IsDescendant(draggedObj, targetObj))
                    {
                        RefreshExplorer();
                        return;
                    }
                    targetObj.Children.Add(draggedObj);
                }

                RefreshExplorer();
            }
        }

        private bool IsDescendant(GameObject parent, GameObject potentialDescendant)
        {
            if (parent == potentialDescendant) return true;
            foreach (var child in parent.Children)
            {
                if (IsDescendant(child, potentialDescendant)) return true;
            }
            return false;
        }

        private void UpdateTitle()
        {
            this.Text = "2DCore";
        }

        private void ViewportPanel_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
                zoom = Math.Min(zoom * 1.15f, MaxZoom);
            else if (e.Delta < 0)
                zoom = Math.Max(zoom / 1.15f, MinZoom);

            viewportPanel.Invalidate();
        }

        private PointF ScreenToWorld(Point screenPt)
        {
            return new PointF(
                (screenPt.X - cameraOffset.X) / zoom,
                (screenPt.Y - cameraOffset.Y) / zoom
            );
        }

        private void AddNewObject(string typeName)
        {
            SaveStateForUndo();
            GameObject obj;

            if (typeName == "Sound" || typeName == "SoundTrigger")
            {
                obj = new SoundObject
                {
                    Name = GetUniqueName(typeName),
                    Size = new Size(50, 50),
                    Position = new Point(-25, -25),
                    Color = System.Drawing.Color.FromArgb(40, 42, 54),
                    ObjectType = typeName
                };
            }
            else
            {
                obj = new GameObject 
                { 
                    Name = GetUniqueName(typeName), 
                    Size = new Size(80, 80),
                    Position = new Point(-40, -40),
                    ObjectType = typeName,
                    Color = System.Drawing.Color.White
                };
            }

            if (targetParentGameObject != null)
            {
                targetParentGameObject.Children.Add(obj);
                if (targetParentGameObject is SoundService && obj is SoundObject)
                {
                    obj.ObjectType = "Sound";
                    obj.Name = GetUniqueName("Sound");
                }
            }
            else
            {
                sceneObjects.Add(obj);
            }

            RefreshExplorer();
            targetParentGameObject = null;
        }

        private void RefreshExplorer()
        {
            explorerTree.Nodes.Clear();

            TreeNode sceneNode = new TreeNode("Scene")
            {
                ImageIndex = 0,
                SelectedImageIndex = 0,
                Tag = null
            };

            foreach (var obj in sceneObjects.Where(o => !(o is SoundService)))
            {
                sceneNode.Nodes.Add(CreateTreeNodeRecursive(obj));
            }

            explorerTree.Nodes.Add(sceneNode);
            sceneNode.Expand();

            foreach (var soundSvc in sceneObjects.OfType<SoundService>())
            {
                TreeNode serviceNode = new TreeNode(soundSvc.Name)
                {
                    Tag = soundSvc,
                    ImageIndex = 5,
                    SelectedImageIndex = 5
                };

                foreach (var child in soundSvc.Children)
                {
                    serviceNode.Nodes.Add(CreateTreeNodeRecursive(child));
                }

                explorerTree.Nodes.Add(serviceNode);
                serviceNode.Expand();
            }

            viewportPanel.Invalidate();
        }

        private TreeNode CreateTreeNodeRecursive(GameObject obj)
        {
            int imgIdx = 2;
            if (obj is SoundService) imgIdx = 5;
            else if (obj is SoundObject) imgIdx = 6;
            else if (obj.ObjectType == "Folder") imgIdx = (obj.Children.Count > 0) ? 4 : 3;
            else if (obj.ObjectType == "Image") imgIdx = 1;

            TreeNode node = new TreeNode(obj.Name)
            {
                Tag = obj,
                ImageIndex = imgIdx,
                SelectedImageIndex = imgIdx
            };

            foreach (var child in obj.Children)
            {
                node.Nodes.Add(CreateTreeNodeRecursive(child));
            }

            return node;
        }

        private void ExplorerTree_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e.Node != null && e.Node.Tag is GameObject obj)
            {
                if (!ModifierKeys.HasFlag(Keys.Control))
                    selectedObjects.Clear();

                if (!selectedObjects.Contains(obj))
                    selectedObjects.Add(obj);

                UpdatePropertyGrid();
            }
            else
            {
                selectedObjects.Clear();
                UpdatePropertyGrid();
            }
            viewportPanel.Invalidate();
        }

        private void UpdatePropertyGrid()
        {
            if (selectedObjects.Count == 1)
                propertiesGrid.SelectedObject = selectedObjects[0];
            else if (selectedObjects.Count > 1)
                propertiesGrid.SelectedObjects = selectedObjects.ToArray();
            else
                propertiesGrid.SelectedObject = null;
        }

        private Dictionary<HandleType, RectangleF> GetHandleRectangles(GameObject obj)
        {
            var handles = new Dictionary<HandleType, RectangleF>();
            if (obj == null || obj is SoundService) return handles;

            float x = obj.Position.X;
            float y = obj.Position.Y;
            float w = obj.Size.Width;
            float h = obj.Size.Height;
            
            float hs = HandleSize / zoom;
            float hs2 = hs / 2f;

            handles[HandleType.TopLeft]     = new RectangleF(x - hs2, y - hs2, hs, hs);
            handles[HandleType.Top]         = new RectangleF(x + w / 2f - hs2, y - hs2, hs, hs);
            handles[HandleType.TopRight]    = new RectangleF(x + w - hs2, y - hs2, hs, hs);
            handles[HandleType.Right]       = new RectangleF(x + w - hs2, y + h / 2f - hs2, hs, hs);
            handles[HandleType.BottomRight] = new RectangleF(x + w - hs2, y + h - hs2, hs, hs);
            handles[HandleType.Bottom]      = new RectangleF(x + w / 2f - hs2, y + h - hs2, hs, hs);
            handles[HandleType.BottomLeft]  = new RectangleF(x - hs2, y + h - hs2, hs, hs);
            handles[HandleType.Left]        = new RectangleF(x - hs2, y + h / 2f - hs2, hs, hs);

            return handles;
        }

        private Cursor GetCursorForHandle(HandleType handle)
        {
            switch (handle)
            {
                case HandleType.TopLeft:
                case HandleType.BottomRight: return Cursors.SizeNWSE;
                case HandleType.TopRight:
                case HandleType.BottomLeft: return Cursors.SizeNESW;
                case HandleType.Top:
                case HandleType.Bottom: return Cursors.SizeNS;
                case HandleType.Left:
                case HandleType.Right: return Cursors.SizeWE;
                default: return Cursors.Default;
            }
        }

        private HandleType HitTestHandle(Point screenPt, out GameObject? targetObj)
        {
            targetObj = null;
            if (selectedObjects.Count != 1) return HandleType.None;

            PointF worldPt = ScreenToWorld(screenPt);
            var obj = selectedObjects[0];
            var handles = GetHandleRectangles(obj);
            foreach (var kvp in handles)
            {
                if (kvp.Value.Contains(worldPt))
                {
                    targetObj = obj;
                    return kvp.Key;
                }
            }
            return HandleType.None;
        }

        private RectangleF GetRectFromPoints(PointF p1, PointF p2)
        {
            float x = Math.Min(p1.X, p2.X);
            float y = Math.Min(p1.Y, p2.Y);
            float w = Math.Abs(p1.X - p2.X);
            float h = Math.Abs(p1.Y - p2.Y);
            return new RectangleF(x, y, w, h);
        }

        private void ViewportPanel_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                isPanning = true;
                panStartMousePos = e.Location;
                panStartOffset = cameraOffset;
                viewportPanel.Cursor = Cursors.SizeAll;
                return;
            }

            if (e.Button != MouseButtons.Left) return;

            PointF worldPt = ScreenToWorld(e.Location);

            GameObject? handleObj;
            HandleType hitHandle = HitTestHandle(e.Location, out handleObj);

            if (hitHandle != HandleType.None && handleObj != null && selectedObjects.Count == 1)
            {
                SaveStateForUndo();
                isResizing = true;
                activeHandle = hitHandle;
                initialMousePos = worldPt;
                initialObjPos = handleObj.Position;
                initialObjSize = handleObj.Size;
                return;
            }

            var allObjs = GetAllObjectsRecursive(sceneObjects.Where(o => !(o is SoundService))).ToList();
            for (int i = allObjs.Count - 1; i >= 0; i--)
            {
                var obj = allObjs[i];
                if (obj.ObjectType == "Folder") continue;

                RectangleF objRect = new RectangleF(obj.Position, obj.Size);

                if (objRect.Contains(worldPt))
                {
                    if (ModifierKeys.HasFlag(Keys.Control))
                    {
                        if (selectedObjects.Contains(obj)) selectedObjects.Remove(obj);
                        else selectedObjects.Add(obj);
                    }
                    else
                    {
                        if (!selectedObjects.Contains(obj))
                        {
                            selectedObjects.Clear();
                            selectedObjects.Add(obj);
                        }
                    }

                    SaveStateForUndo();
                    isDragging = true;
                    dragStartWorldPos = worldPt;
                    dragStartObjectPositions.Clear();
                    foreach (var selectedObj in selectedObjects)
                    {
                        dragStartObjectPositions[selectedObj] = selectedObj.Position;
                    }

                    UpdatePropertyGrid();
                    viewportPanel.Invalidate();
                    return;
                }
            }

            if (!ModifierKeys.HasFlag(Keys.Control))
            {
                selectedObjects.Clear();
                UpdatePropertyGrid();
            }

            isBoxSelecting = true;
            boxSelectStart = worldPt;
            boxSelectCurrent = worldPt;
            viewportPanel.Invalidate();
        }

        private void ViewportPanel_MouseMove(object? sender, MouseEventArgs e)
        {
            if (isPanning)
            {
                float dx = e.X - panStartMousePos.X;
                float dy = e.Y - panStartMousePos.Y;
                cameraOffset = new PointF(panStartOffset.X + dx, panStartOffset.Y + dy);
                viewportPanel.Invalidate();
                return;
            }

            PointF worldPt = ScreenToWorld(e.Location);

            if (isBoxSelecting)
            {
                boxSelectCurrent = worldPt;
                viewportPanel.Invalidate();
                return;
            }

            if (!isDragging && !isResizing)
            {
                GameObject? dummy;
                HandleType hoverHandle = HitTestHandle(e.Location, out dummy);
                viewportPanel.Cursor = GetCursorForHandle(hoverHandle);
            }

            if (isResizing && selectedObjects.Count == 1)
            {
                var targetObj = selectedObjects[0];
                float dx = worldPt.X - initialMousePos.X;
                float dy = worldPt.Y - initialMousePos.Y;

                int newX = initialObjPos.X;
                int newY = initialObjPos.Y;
                int newW = initialObjSize.Width;
                int newH = initialObjSize.Height;

                const int minSize = 15;

                if (activeHandle == HandleType.Right || activeHandle == HandleType.TopRight || activeHandle == HandleType.BottomRight)
                    newW = (int)Math.Max(minSize, initialObjSize.Width + dx);

                if (activeHandle == HandleType.Bottom || activeHandle == HandleType.BottomLeft || activeHandle == HandleType.BottomRight)
                    newH = (int)Math.Max(minSize, initialObjSize.Height + dy);

                if (activeHandle == HandleType.Left || activeHandle == HandleType.TopLeft || activeHandle == HandleType.BottomLeft)
                {
                    int maxDx = initialObjSize.Width - minSize;
                    int appliedDx = (int)Math.Min(dx, maxDx);
                    newX = initialObjPos.X + appliedDx;
                    newW = initialObjSize.Width - appliedDx;
                }

                if (activeHandle == HandleType.Top || activeHandle == HandleType.TopLeft || activeHandle == HandleType.TopRight)
                {
                    int maxDy = initialObjSize.Height - minSize;
                    int appliedDy = (int)Math.Min(dy, maxDy);
                    newY = initialObjPos.Y + appliedDy;
                    newH = initialObjSize.Height - appliedDy;
                }

                targetObj.Position = new Point(newX, newY);
                targetObj.Size = new Size(newW, newH);

                viewportPanel.Invalidate();
            }
            else if (isDragging && selectedObjects.Count > 0)
            {
                float deltaX = worldPt.X - dragStartWorldPos.X;
                float deltaY = worldPt.Y - dragStartWorldPos.Y;

                foreach (var obj in selectedObjects)
                {
                    if (dragStartObjectPositions.TryGetValue(obj, out Point startPos))
                    {
                        obj.Position = new Point((int)(startPos.X + deltaX), (int)(startPos.Y + deltaY));
                    }
                }
                viewportPanel.Invalidate();
            }
        }

        private void ViewportPanel_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                isPanning = false;
                viewportPanel.Cursor = Cursors.Default;
                return;
            }

            if (isBoxSelecting)
            {
                isBoxSelecting = false;
                RectangleF selectionRect = GetRectFromPoints(boxSelectStart, boxSelectCurrent);

                foreach (var obj in GetAllObjectsRecursive(sceneObjects.Where(o => !(o is SoundService))))
                {
                    if (obj.ObjectType == "Folder") continue;

                    RectangleF objRect = new RectangleF(obj.Position, obj.Size);
                    if (selectionRect.IntersectsWith(objRect))
                    {
                        if (!selectedObjects.Contains(obj)) selectedObjects.Add(obj);
                    }
                }

                UpdatePropertyGrid();
            }

            isDragging = false;
            isResizing = false;
            activeHandle = HandleType.None;
            viewportPanel.Cursor = Cursors.Default;

            propertiesGrid.Refresh();
            viewportPanel.Invalidate();
        }

        private void ViewportPanel_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(bgDark);

            g.TranslateTransform(cameraOffset.X, cameraOffset.Y);
            g.ScaleTransform(zoom, zoom);

            DrawGridAndAxes(g);

            foreach (var obj in GetAllObjectsRecursive(sceneObjects.Where(o => !(o is SoundService))))
            {
                if (obj.ObjectType == "Folder") continue;

                Rectangle rect = new Rectangle(obj.Position, obj.Size);
                float alpha = Math.Clamp(1.0f - obj.Transparency, 0.0f, 1.0f);
                int alphaByte = (int)(alpha * 255);

                if (obj is SoundObject)
                {
                    if (soundTriggerIcon != null)
                    {
                        if (alpha < 0.99f)
                        {
                            ColorMatrix colorMatrix = new ColorMatrix();
                            colorMatrix.Matrix33 = alpha;
                            using (ImageAttributes imgAttributes = new ImageAttributes())
                            {
                                imgAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                                g.DrawImage(soundTriggerIcon, rect, 0, 0, soundTriggerIcon.Width, soundTriggerIcon.Height, GraphicsUnit.Pixel, imgAttributes);
                            }
                        }
                        else
                        {
                            g.DrawImage(soundTriggerIcon, rect);
                        }
                    }
                }
                else if (obj.Texture != null)
                {
                    if (alpha < 0.99f)
                    {
                        ColorMatrix colorMatrix = new ColorMatrix();
                        colorMatrix.Matrix33 = alpha;
                        using (ImageAttributes imgAttributes = new ImageAttributes())
                        {
                            imgAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                            g.DrawImage(obj.Texture, rect, 0, 0, obj.Texture.Width, obj.Texture.Height, GraphicsUnit.Pixel, imgAttributes);
                        }
                    }
                    else
                    {
                        g.DrawImage(obj.Texture, rect);
                    }
                }
                else
                {
                    System.Drawing.Color renderColor = System.Drawing.Color.FromArgb(alphaByte, obj.Color.R, obj.Color.G, obj.Color.B);
                    using (Brush brush = new SolidBrush(renderColor))
                    {
                        g.FillRectangle(brush, rect);
                    }
                }

                if (selectedObjects.Contains(obj))
                {
                    using (Pen pen = new Pen(accentBlue, 2f / zoom))
                    {
                        g.DrawRectangle(pen, rect);
                    }

                    if (selectedObjects.Count == 1)
                    {
                        var handles = GetHandleRectangles(obj);
                        using (Pen borderPen = new Pen(System.Drawing.Color.FromArgb(20, 20, 20), 1f / zoom))
                        using (Brush fillBrush = new SolidBrush(System.Drawing.Color.White))
                        {
                            foreach (var handleRect in handles.Values)
                            {
                                g.FillRectangle(fillBrush, handleRect);
                                g.DrawRectangle(borderPen, handleRect.X, handleRect.Y, handleRect.Width, handleRect.Height);
                            }
                        }
                    }
                }
            }

            if (isBoxSelecting)
            {
                RectangleF marquee = GetRectFromPoints(boxSelectStart, boxSelectCurrent);
                using (Brush fill = new SolidBrush(System.Drawing.Color.FromArgb(40, 52, 120, 246)))
                using (Pen border = new Pen(accentBlue, 1f / zoom) { DashStyle = DashStyle.Dash })
                {
                    g.FillRectangle(fill, marquee);
                    g.DrawRectangle(border, marquee.X, marquee.Y, marquee.Width, marquee.Height);
                }
            }
        }

        #region Project & File Management Systems
        private void SetModified(bool modified)
        {
            isModified = modified;
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            string fileName = !string.IsNullOrEmpty(currentProjectPath) 
                ? Path.GetFileName(currentProjectPath) 
                : currentProjectName;
            this.Text = $"2DCore Engine - {fileName}{(isModified ? " *" : "")}";
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!PromptSaveIfModified())
            {
                e.Cancel = true;
            }
        }

        private bool PromptSaveIfModified()
        {
            if (!isModified) return true;

            DialogResult result = MessageBox.Show(
                "Проект содержит несохранённые изменения. Сохранить их перед продолжением?",
                "Несохранённые изменения",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                return SaveProjectCommand();
            }
            else if (result == DialogResult.No)
            {
                return true;
            }

            return false;
        }

        private bool NewProjectCommand()
        {
            if (!PromptSaveIfModified()) return false;

            sceneObjects.Clear();
            selectedObjects.Clear();
            undoStack.Clear();
            redoStack.Clear();

            sceneObjects.Add(new SoundService());

            currentProjectPath = null;
            currentProjectName = "New Project";
            SetModified(false);

            RefreshExplorer();
            UpdatePropertyGrid();
            viewportPanel.Invalidate();

            Log("Создан новый проект.", LogType.Info);
            return true;
        }

        private bool OpenProjectCommand()
        {
            if (!PromptSaveIfModified()) return false;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "2D Engine Files (*.2dproj;*.2dscene)|*.2dproj;*.2dscene|2D Project (*.2dproj)|*.2dproj|2D Scene (*.2dscene)|*.2dscene";
                ofd.Title = "Открыть проект или сцену";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    return PerformOpenProject(ofd.FileName);
                }
            }
            return false;
        }

        private bool SaveProjectCommand()
        {
            if (string.IsNullOrEmpty(currentProjectPath))
            {
                return SaveProjectAsCommand();
            }
            return PerformSaveProject(currentProjectPath);
        }

        private bool SaveProjectAsCommand()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "2D Engine Project (*.2dproj)|*.2dproj|2D Engine Scene (*.2dscene)|*.2dscene";
                sfd.Title = "Сохранить проект как...";
                sfd.FileName = currentProjectName;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    return PerformSaveProject(sfd.FileName);
                }
            }
            return false;
        }

        private bool SafeWriteText(string filePath, string content)
        {
            string tempFilePath = filePath + ".tmp";
            try
            {
                string dir = Path.GetDirectoryName(filePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(tempFilePath, content);

                if (File.Exists(filePath))
                {
                    File.Replace(tempFilePath, filePath, null);
                }
                else
                {
                    File.Move(tempFilePath, filePath);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log($"Ошибка записи файла '{filePath}': {ex.Message}", LogType.Error);
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
                catch { }
                return false;
            }
        }

        private bool PerformSaveProject(string filePath)
        {
            try
            {
                string projDir = Path.GetDirectoryName(filePath) ?? string.Empty;
                string ext = Path.GetExtension(filePath).ToLower();

                if (ext == ".2dproj")
                {
                    string scenesRelDir = "scenes";
                    string scenesAbsDir = Path.Combine(projDir, scenesRelDir);
                    if (!Directory.Exists(scenesAbsDir))
                    {
                        Directory.CreateDirectory(scenesAbsDir);
                    }

                    string sceneRelFile = Path.Combine(scenesRelDir, "main.2dscene").Replace('\\', '/');
                    string sceneAbsFile = Path.Combine(projDir, "scenes", "main.2dscene");

                    SceneDataDTO sceneDto = new SceneDataDTO
                    {
                        FormatVersion = 1,
                        SceneName = "MainScene",
                        Objects = sceneObjects.Select(o => ConvertToDTO(o, null, projDir)).ToList()
                    };

                    string sceneJson = JsonSerializer.Serialize(sceneDto, jsonOptions);
                    if (!SafeWriteText(sceneAbsFile, sceneJson))
                    {
                        return false;
                    }

                    ProjectDataDTO projDto = new ProjectDataDTO
                    {
                        FormatVersion = 1,
                        ProjectName = Path.GetFileNameWithoutExtension(filePath),
                        StartScene = sceneRelFile,
                        Scenes = new List<string> { sceneRelFile },
                        Settings = new ProjectSettingsDTO
                        {
                            ViewportWidth = viewportPanel.Width,
                            ViewportHeight = viewportPanel.Height,
                            BackgroundColorHex = ColorTranslator.ToHtml(bgDark)
                        }
                    };

                    string projJson = JsonSerializer.Serialize(projDto, jsonOptions);
                    if (!SafeWriteText(filePath, projJson))
                    {
                        return false;
                    }
                }
                else
                {
                    SceneDataDTO sceneDto = new SceneDataDTO
                    {
                        FormatVersion = 1,
                        SceneName = Path.GetFileNameWithoutExtension(filePath),
                        Objects = sceneObjects.Select(o => ConvertToDTO(o, null, projDir)).ToList()
                    };

                    string sceneJson = JsonSerializer.Serialize(sceneDto, jsonOptions);
                    if (!SafeWriteText(filePath, sceneJson))
                    {
                        return false;
                    }
                }

                currentProjectPath = filePath;
                currentProjectName = Path.GetFileNameWithoutExtension(filePath);
                SetModified(false);
                Log($"Успешно сохранено: '{filePath}'", LogType.Info);
                return true;
            }
            catch (Exception ex)
            {
                Log($"Сбой при сохранении проекта: {ex.Message}", LogType.Error);
                return false;
            }
        }

        private bool PerformOpenProject(string filePath)
        {
            try
            {
                string projDir = Path.GetDirectoryName(filePath) ?? string.Empty;
                string ext = Path.GetExtension(filePath).ToLower();

                List<GameObjectDTO> loadedObjectsDTO = new List<GameObjectDTO>();

                if (ext == ".2dproj")
                {
                    string projJson = File.ReadAllText(filePath);
                    ProjectDataDTO? projDto = JsonSerializer.Deserialize<ProjectDataDTO>(projJson, jsonOptions);

                    if (projDto == null)
                    {
                        Log($"Не удалось прочитать файл проекта '{filePath}'.", LogType.Error);
                        return false;
                    }

                    string sceneRelPath = projDto.StartScene;
                    string sceneAbsPath = Path.IsPathRooted(sceneRelPath) ? sceneRelPath : Path.Combine(projDir, sceneRelPath);

                    if (File.Exists(sceneAbsPath))
                    {
                        string sceneJson = File.ReadAllText(sceneAbsPath);
                        SceneDataDTO? sceneDto = JsonSerializer.Deserialize<SceneDataDTO>(sceneJson, jsonOptions);
                        if (sceneDto != null)
                        {
                            loadedObjectsDTO = sceneDto.Objects;
                        }
                    }
                    else
                    {
                        Log($"Файл стартовой сцены не найден: '{sceneAbsPath}'.", LogType.Warning);
                    }
                }
                else
                {
                    string sceneJson = File.ReadAllText(filePath);
                    SceneDataDTO? sceneDto = JsonSerializer.Deserialize<SceneDataDTO>(sceneJson, jsonOptions);
                    if (sceneDto != null)
                    {
                        loadedObjectsDTO = sceneDto.Objects;
                    }
                }

                HashSet<Guid> loadedGuids = new HashSet<Guid>();
                List<GameObject> newSceneObjects = new List<GameObject>();

                foreach (var dto in loadedObjectsDTO)
                {
                    newSceneObjects.Add(LoadObjectFromDTO(dto, projDir, loadedGuids));
                }

                if (!newSceneObjects.OfType<SoundService>().Any())
                {
                    newSceneObjects.Add(new SoundService());
                }

                sceneObjects = newSceneObjects;
                selectedObjects.Clear();
                undoStack.Clear();
                redoStack.Clear();

                currentProjectPath = filePath;
                currentProjectName = Path.GetFileNameWithoutExtension(filePath);
                SetModified(false);

                RefreshExplorer();
                UpdatePropertyGrid();
                viewportPanel.Invalidate();

                Log($"Проект успешно загружен: '{filePath}'", LogType.Info);
                return true;
            }
            catch (Exception ex)
            {
                Log($"Ошибка при загрузке файла: {ex.Message}", LogType.Error);
                return false;
            }
        }

        private GameObjectDTO ConvertToDTO(GameObject obj, Guid? parentId, string projectDir)
        {
            var dto = new GameObjectDTO
            {
                Id = obj.Id,
                ParentId = parentId,
                Name = obj.Name,
                ObjectType = obj.ObjectType,
                Components = new List<ComponentDTO>()
            };

            // Transform Component
            dto.Components.Add(new TransformComponentDTO
            {
                X = obj.Position.X,
                Y = obj.Position.Y,
                Width = obj.Size.Width,
                Height = obj.Size.Height,
                Transparency = obj.Transparency
            });

            // Render Component
            string relTexturePath = string.Empty;
            if (!string.IsNullOrEmpty(obj.TexturePath))
            {
                relTexturePath = GetRelativePath(obj.TexturePath, projectDir);
            }

            dto.Components.Add(new RenderComponentDTO
            {
                ColorHex = ColorTranslator.ToHtml(obj.Color),
                TexturePath = relTexturePath
            });

            // Sound Component
            if (obj is SoundObject soundObj)
            {
                string relAudioPath = string.Empty;
                if (!string.IsNullOrEmpty(soundObj.FilePath))
                {
                    relAudioPath = GetRelativePath(soundObj.FilePath, projectDir);
                }

                dto.Components.Add(new SoundComponentDTO
                {
                    AudioFilePath = relAudioPath,
                    Volume = soundObj.Volume
                });
            }

            // Children recursion
            foreach (var child in obj.Children)
            {
                dto.Children.Add(ConvertToDTO(child, obj.Id, projectDir));
            }

            return dto;
        }

        private GameObject LoadObjectFromDTO(GameObjectDTO dto, string projectDir, HashSet<Guid> loadedGuids)
        {
            Guid validId = dto.Id;
            if (validId == Guid.Empty || loadedGuids.Contains(validId))
            {
                validId = Guid.NewGuid();
                Log($"Предупреждение: Обнаружен дубликат/пустой GUID для '{dto.Name}'. Сгенерирован новый GUID: {validId}", LogType.Warning);
            }
            loadedGuids.Add(validId);

            GameObject obj;
            if (dto.ObjectType == "SoundService")
            {
                obj = new SoundService();
            }
            else if (dto.ObjectType == "SoundTrigger" || dto.ObjectType == "Sound")
            {
                obj = new SoundObject();
            }
            else
            {
                obj = new GameObject();
            }

            obj.Id = validId;
            obj.Name = dto.Name;
            obj.ObjectType = dto.ObjectType;

            foreach (var comp in dto.Components)
            {
                if (comp is TransformComponentDTO transformComp)
                {
                    obj.Position = new Point(transformComp.X, transformComp.Y);
                    obj.Size = new Size(transformComp.Width, transformComp.Height);
                    obj.Transparency = transformComp.Transparency;
                }
                else if (comp is RenderComponentDTO renderComp)
                {
                    if (!string.IsNullOrEmpty(renderComp.ColorHex))
                    {
                        try
                        {
                            obj.Color = ColorTranslator.FromHtml(renderComp.ColorHex);
                        }
                        catch { }
                    }

                    if (!string.IsNullOrEmpty(renderComp.TexturePath))
                    {
                        string absTexturePath = Path.IsPathRooted(renderComp.TexturePath)
                            ? renderComp.TexturePath
                            : Path.Combine(projectDir, renderComp.TexturePath);

                        obj.TexturePath = renderComp.TexturePath;

                        if (File.Exists(absTexturePath))
                        {
                            try
                            {
                                using (var img = Image.FromFile(absTexturePath))
                                {
                                    obj.Texture = new Bitmap(img);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log($"Предупреждение: Ошибка загрузки текстуры '{absTexturePath}' для '{obj.Name}': {ex.Message}", LogType.Warning);
                            }
                        }
                        else
                        {
                            Log($"Предупреждение: Текстурный файл не найден: '{absTexturePath}' для '{obj.Name}'.", LogType.Warning);
                        }
                    }
                }
                else if (comp is SoundComponentDTO soundComp && obj is SoundObject soundObj)
                {
                    soundObj.Volume = soundComp.Volume;
                    if (!string.IsNullOrEmpty(soundComp.AudioFilePath))
                    {
                        string absAudioPath = Path.IsPathRooted(soundComp.AudioFilePath)
                            ? soundComp.AudioFilePath
                            : Path.Combine(projectDir, soundComp.AudioFilePath);

                        soundObj.FilePath = absAudioPath;
                        if (!File.Exists(absAudioPath))
                        {
                            Log($"Предупреждение: Аудиофайл не найден: '{absAudioPath}' для '{soundObj.Name}'.", LogType.Warning);
                        }
                    }
                }
            }

            foreach (var childDto in dto.Children)
            {
                obj.Children.Add(LoadObjectFromDTO(childDto, projectDir, loadedGuids));
            }

            return obj;
        }

        private string GetRelativePath(string fullPath, string basePath)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(basePath))
                return fullPath;

            try
            {
                string baseDir = basePath;
                if (!baseDir.EndsWith(Path.DirectorySeparatorChar.ToString()) && !baseDir.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    baseDir += Path.DirectorySeparatorChar;
                }

                Uri baseUri = new Uri(baseDir);
                Uri fullUri = new Uri(fullPath);
                if (baseUri.Scheme != fullUri.Scheme) return fullPath;

                Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
                string relativePath = Uri.UnescapeDataString(relativeUri.ToString());
                return relativePath.Replace('/', Path.DirectorySeparatorChar);
            }
            catch
            {
                return fullPath;
            }
        }

        private void AssignNewGuids(GameObject obj)
        {
            obj.Id = Guid.NewGuid();
            foreach (var child in obj.Children)
            {
                AssignNewGuids(child);
            }
        }
        #endregion

        private void DrawGridAndAxes(Graphics g)
        {
            int gridSize = 40;

            PointF topLeft = ScreenToWorld(Point.Empty);
            PointF bottomRight = ScreenToWorld(new Point(viewportPanel.Width, viewportPanel.Height));

            int startX = ((int)Math.Floor(topLeft.X / gridSize) - 1) * gridSize;
            int endX = ((int)Math.Ceiling(bottomRight.X / gridSize) + 1) * gridSize;
            int startY = ((int)Math.Floor(topLeft.Y / gridSize) - 1) * gridSize;
            int endY = ((int)Math.Ceiling(bottomRight.Y / gridSize) + 1) * gridSize;

            using (Pen lightPen = new Pen(System.Drawing.Color.FromArgb(26, 28, 35), 1f / zoom))
            using (Pen darkPen = new Pen(System.Drawing.Color.FromArgb(38, 40, 52), 1f / zoom))
            {
                for (int x = startX; x <= endX; x += gridSize)
                {
                    Pen pen = (x % (gridSize * 4) == 0) ? darkPen : lightPen;
                    g.DrawLine(pen, x, startY, x, endY);
                }

                for (int y = startY; y <= endY; y += gridSize)
                {
                    Pen pen = (y % (gridSize * 4) == 0) ? darkPen : lightPen;
                    g.DrawLine(pen, startX, y, endX, y);
                }
            }

            using (Pen axisX = new Pen(System.Drawing.Color.FromArgb(220, 70, 70), 2f / zoom))
            using (Pen axisY = new Pen(System.Drawing.Color.FromArgb(70, 180, 70), 2f / zoom))
            {
                g.DrawLine(axisX, startX, 0, endX, 0);
                g.DrawLine(axisY, 0, startY, 0, endY);
            }
        }
    }
}