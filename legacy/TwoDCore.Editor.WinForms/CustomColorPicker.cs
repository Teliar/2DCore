using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace _2DCore
{
    public class DarkDropdownColorEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider? provider, object? value)
        {
            var editorService = (IWindowsFormsEditorService?)provider?.GetService(typeof(IWindowsFormsEditorService));
            if (editorService != null && value is Color currentColor)
            {
                var pickerControl = new DarkColorPickerControl(currentColor, editorService, (newColor) =>
                {
                    if (context?.Instance != null && context.PropertyDescriptor != null)
                    {
                        context.PropertyDescriptor.SetValue(context.Instance, newColor);
                    }
                });

                editorService.DropDownControl(pickerControl);
                return pickerControl.SelectedColor;
            }
            return value;
        }
    }

    public class DarkColorPickerControl : UserControl
    {
        public Color SelectedColor { get; private set; }

        private readonly PictureBox spectrumBox;
        private readonly PictureBox lightnessBox;
        private readonly PictureBox previewBox;
        private readonly TextBox hexTextBox;
        
        private readonly IWindowsFormsEditorService? _editorService;
        private readonly Action<Color>? _onColorChanged;

        private float hue = 0f;          
        private float saturation = 1f;   
        private float valueVal = 1f;     

        private bool isUpdating = false;

        public DarkColorPickerControl(Color initialColor, IWindowsFormsEditorService? editorService, Action<Color>? onColorChanged)
        {
            _editorService = editorService;
            _onColorChanged = onColorChanged;
            SelectedColor = initialColor;

            this.Size = new Size(260, 230);
            this.BackColor = Color.FromArgb(24, 25, 30);
            this.ForeColor = Color.FromArgb(220, 222, 230);
            this.Font = new Font("Segoe UI", 9f);

            spectrumBox = new PictureBox();
            lightnessBox = new PictureBox();
            previewBox = new PictureBox();
            hexTextBox = new TextBox();

            InitializeComponents();
            SetColor(initialColor);
        }

        private void InitializeComponents()
        {
            spectrumBox.Location = new Point(10, 10);
            spectrumBox.Size = new Size(180, 150);
            spectrumBox.BorderStyle = BorderStyle.FixedSingle;
            spectrumBox.Cursor = Cursors.Cross;
            spectrumBox.Paint += SpectrumBox_Paint;
            spectrumBox.MouseDown += SpectrumBox_Mouse;
            spectrumBox.MouseMove += (s, e) => { if (e?.Button == MouseButtons.Left) SpectrumBox_Mouse(s, e); };

            lightnessBox.Location = new Point(200, 10);
            lightnessBox.Size = new Size(20, 150);
            lightnessBox.BorderStyle = BorderStyle.FixedSingle;
            lightnessBox.Cursor = Cursors.Hand;
            lightnessBox.Paint += LightnessBox_Paint;
            lightnessBox.MouseDown += LightnessBox_Mouse;
            lightnessBox.MouseMove += (s, e) => { if (e?.Button == MouseButtons.Left) LightnessBox_Mouse(s, e); };

            previewBox.Location = new Point(10, 170);
            previewBox.Size = new Size(40, 23);
            previewBox.BorderStyle = BorderStyle.FixedSingle;

            Label hexLabel = new Label { Text = "HEX:", Location = new Point(60, 173), AutoSize = true, ForeColor = Color.Gray };
            
            hexTextBox.Location = new Point(95, 170);
            hexTextBox.Size = new Size(65, 23);
            hexTextBox.BackColor = Color.FromArgb(18, 19, 23);
            hexTextBox.ForeColor = Color.FromArgb(220, 222, 230);
            hexTextBox.BorderStyle = BorderStyle.FixedSingle;
            hexTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ParseHex(hexTextBox.Text);
                    e.SuppressKeyPress = true;
                }
            };

            Button btnOk = new Button
            {
                Text = "OK",
                Location = new Point(170, 169),
                Size = new Size(50, 25),
                BackColor = Color.FromArgb(18, 19, 23),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnOk.Click += (s, e) => _editorService?.CloseDropDown();

            this.Controls.Add(spectrumBox);
            this.Controls.Add(lightnessBox);
            this.Controls.Add(previewBox);
            this.Controls.Add(hexLabel);
            this.Controls.Add(hexTextBox);
            this.Controls.Add(btnOk);
        }

        private void SpectrumBox_Paint(object? sender, PaintEventArgs e)
        {
            if (e == null) return;
            Graphics g = e.Graphics;
            Rectangle rect = spectrumBox.ClientRectangle;

            using (LinearGradientBrush hueBrush = new LinearGradientBrush(rect, Color.White, Color.White, 0f))
            {
                ColorBlend blend = new ColorBlend();
                blend.Positions = new float[] { 0, 1/6f, 2/6f, 3/6f, 4/6f, 5/6f, 1f };
                blend.Colors = new Color[] { 
                    HsvToRgb(0, 1, valueVal), 
                    HsvToRgb(60, 1, valueVal), 
                    HsvToRgb(120, 1, valueVal), 
                    HsvToRgb(180, 1, valueVal), 
                    HsvToRgb(240, 1, valueVal), 
                    HsvToRgb(300, 1, valueVal), 
                    HsvToRgb(360, 1, valueVal) 
                };
                hueBrush.InterpolationColors = blend;
                g.FillRectangle(hueBrush, rect);
            }

            using (LinearGradientBrush satBrush = new LinearGradientBrush(rect, Color.Transparent, Color.White, 90f))
            {
                g.FillRectangle(satBrush, rect);
            }

            int cx = (int)((hue / 360f) * rect.Width);
            int cy = (int)((1f - saturation) * rect.Height);
            g.DrawEllipse(Pens.Black, cx - 4, cy - 4, 8, 8);
            g.DrawEllipse(Pens.White, cx - 3, cy - 3, 6, 6);
        }

        private void LightnessBox_Paint(object? sender, PaintEventArgs e)
        {
            if (e == null) return;
            Graphics g = e.Graphics;
            Rectangle rect = lightnessBox.ClientRectangle;

            Color topColor = HsvToRgb(hue, saturation, 1f);
            using (LinearGradientBrush brush = new LinearGradientBrush(rect, topColor, Color.Black, 90f))
            {
                g.FillRectangle(brush, rect);
            }

            int sy = (int)((1f - valueVal) * rect.Height);
            g.DrawLine(new Pen(Color.White, 2), 0, sy, rect.Width, sy);
            g.DrawLine(Pens.Black, 0, sy - 1, rect.Width, sy - 1);
            g.DrawLine(Pens.Black, 0, sy + 1, rect.Width, sy + 1);
        }

        private void SpectrumBox_Mouse(object? sender, MouseEventArgs? e)
        {
            if (e == null) return;
            hue = Math.Clamp((float)e.X / spectrumBox.Width, 0f, 1f) * 360f;
            saturation = Math.Clamp(1f - ((float)e.Y / spectrumBox.Height), 0f, 1f);
            UpdateColor();
        }

        private void LightnessBox_Mouse(object? sender, MouseEventArgs? e)
        {
            if (e == null) return;
            valueVal = Math.Clamp(1f - ((float)e.Y / lightnessBox.Height), 0f, 1f);
            UpdateColor();
        }

        private void UpdateColor()
        {
            SelectedColor = HsvToRgb(hue, saturation, valueVal);
            UpdateUI();
            _onColorChanged?.Invoke(SelectedColor);
        }

        private void SetColor(Color c)
        {
            SelectedColor = c;
            RgbToHsv(c, out hue, out saturation, out valueVal);
            UpdateUI();
        }

        private void ParseHex(string hex)
        {
            try
            {
                Color c = ColorTranslator.FromHtml(hex.StartsWith("#") ? hex : "#" + hex);
                SetColor(c);
                _onColorChanged?.Invoke(SelectedColor);
            }
            catch { UpdateUI(); }
        }

        private void UpdateUI()
        {
            if (isUpdating) return;
            isUpdating = true;

            previewBox.BackColor = SelectedColor;
            hexTextBox.Text = ColorTranslator.ToHtml(SelectedColor);

            spectrumBox.Invalidate();
            lightnessBox.Invalidate();

            isUpdating = false;
        }

        private Color HsvToRgb(float h, float s, float v)
        {
            int hi = Convert.ToInt32(Math.Floor(h / 60)) % 6;
            float f = h / 60 - (float)Math.Floor(h / 60);

            v = v * 255;
            int pv = Convert.ToInt32(v * (1 - s));
            int qv = Convert.ToInt32(v * (1 - f * s));
            int tv = Convert.ToInt32(v * (1 - (1 - f) * s));
            int vv = Convert.ToInt32(v);

            switch (hi)
            {
                case 0: return Color.FromArgb(255, vv, tv, pv);
                case 1: return Color.FromArgb(255, qv, vv, pv);
                case 2: return Color.FromArgb(255, pv, vv, tv);
                case 3: return Color.FromArgb(255, pv, qv, vv);
                case 4: return Color.FromArgb(255, tv, pv, vv);
                default: return Color.FromArgb(255, vv, pv, qv);
            }
        }

        private void RgbToHsv(Color color, out float h, out float s, out float v)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            h = color.GetHue();
            s = (max == 0) ? 0 : 1f - (1f * min / max);
            v = max / 255f;
        }
    }
}