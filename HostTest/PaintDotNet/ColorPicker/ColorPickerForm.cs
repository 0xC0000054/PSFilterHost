/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;
using System.Windows.Forms;
using PaintDotNet;

namespace HostTest
{
    internal sealed class ColorPickerForm : Form
    {
        private Label redLabel;
        private Label blueLabel;
        private Label greenLabel;
        private Label hueLabel;

        private NumericUpDown redUpDown;
        private NumericUpDown greenUpDown;
        private NumericUpDown blueUpDown;
        private NumericUpDown hueUpDown;
        private NumericUpDown valueUpDown;
        private NumericUpDown saturationUpDown;

        private System.ComponentModel.Container components = null;
        private Label saturationLabel;
        private Label valueLabel;
        private ColorGradientControl valueGradientControl;
        private ColorWheel colorWheel;

        private int ignoreChangedEvents = 0;

        private System.Windows.Forms.Label hexLabel;
        private System.Windows.Forms.TextBox hexBox;
        private uint ignore = 0;
        private HeaderLabel rgbHeader;
        private HeaderLabel hsvHeader;

        private ColorGradientControl hueGradientControl;
        private ColorGradientControl saturationGradientControl;
        private ColorGradientControl redGradientControl;
        private ColorGradientControl greenGradientControl;
        private ColorGradientControl blueGradientControl;

        private ColorRectangleControl colorDisplayWidget;
        private HeaderLabel swatchHeader;
        private SwatchControl swatchControl;
        private Button okBtn;
        private Button cancelBtn;
        private Label promptLbl;

        private static readonly Color[] paletteColors;

        static ColorPickerForm()
        {
            unchecked
            {
                paletteColors = new Color[64]
                {
                    // row 1
                    Color.FromArgb((int)0xFF000000),
                    Color.FromArgb((int)0xFF404040),
                    Color.FromArgb((int)0xFFFF0000),
                    Color.FromArgb((int)0xFFFF6A00),
                    Color.FromArgb((int)0xFFFFD800),
                    Color.FromArgb((int)0xFFB6FF00),
                    Color.FromArgb((int)0xFF4CFF00),
                    Color.FromArgb((int)0xFF00FF21),
                    Color.FromArgb((int)0xFF00FF90),
                    Color.FromArgb((int)0xFF00FFFF),
                    Color.FromArgb((int)0xFF0094FF),
                    Color.FromArgb((int)0xFF0026FF),
                    Color.FromArgb((int)0xFF4800FF),
                    Color.FromArgb((int)0xFFB200FF),
                    Color.FromArgb((int)0xFFFF00DC),
                    Color.FromArgb((int)0xFFFF006E),
                    // row 2
                    Color.FromArgb((int)0xFFFFFFFF),
                    Color.FromArgb((int)0xFF808080),
                    Color.FromArgb((int)0xFF7F0000),
                    Color.FromArgb((int)0xFF7F3300),
                    Color.FromArgb((int)0xFF7F6A00),
                    Color.FromArgb((int)0xFF5B7F00),
                    Color.FromArgb((int)0xFF267F00),
                    Color.FromArgb((int)0xFF007F0E),
                    Color.FromArgb((int)0xFF007F46),
                    Color.FromArgb((int)0xFF007F7F),
                    Color.FromArgb((int)0xFF004A7F),
                    Color.FromArgb((int)0xFF00137F),
                    Color.FromArgb((int)0xFF21007F),
                    Color.FromArgb((int)0xFF57007F),
                    Color.FromArgb((int)0xFF7F006E),
                    Color.FromArgb((int)0xFF7F006E),
                    // row 3
                    Color.FromArgb((int)0xFFA0A0A0),
                    Color.FromArgb((int)0xFF303030),
                    Color.FromArgb((int)0xFFFF7F7F),
                    Color.FromArgb((int)0xFFFFB27F),
                    Color.FromArgb((int)0xFFFFE97F),
                    Color.FromArgb((int)0xFFDAFF7F),
                    Color.FromArgb((int)0xFFA5FF7F),
                    Color.FromArgb((int)0xFF7FFF8E),
                    Color.FromArgb((int)0xFF7FFFC5),
                    Color.FromArgb((int)0xFF7FFFFF),
                    Color.FromArgb((int)0xFF7FC9FF),
                    Color.FromArgb((int)0xFF7F92FF),
                    Color.FromArgb((int)0xFFA17FFF),
                    Color.FromArgb((int)0xFFD67FFF),
                    Color.FromArgb((int)0xFFFF7FED),
                    Color.FromArgb((int)0xFFFF7FB6),
                    // row 4
                    Color.FromArgb((int)0xFFC0C0C0),
                    Color.FromArgb((int)0xFF606060),
                    Color.FromArgb((int)0xFF7F3F3F),
                    Color.FromArgb((int)0xFF7F593F),
                    Color.FromArgb((int)0xFF7F743F),
                    Color.FromArgb((int)0xFF6D7F3F),
                    Color.FromArgb((int)0xFF527F3F),
                    Color.FromArgb((int)0xFF3F7F47),
                    Color.FromArgb((int)0xFF3F7F62),
                    Color.FromArgb((int)0xFF3F7F7F),
                    Color.FromArgb((int)0xFF3F647F),
                    Color.FromArgb((int)0xFF3F497F),
                    Color.FromArgb((int)0xFF503F7F),
                    Color.FromArgb((int)0xFF6B3F7F),
                    Color.FromArgb((int)0xFF7F3F76),
                    Color.FromArgb((int)0xFF7F3F5B)
                };
            }
        }

        private bool IgnoreChangedEvents
        {
            get
            {
                return ignoreChangedEvents != 0;
            }
        }

        private Color color;
        public Color Color
        {
            get
            {
                return color;
            }

            set
            {
                if (color != value)
                {
                    color = value;

                    ignore++;

                    // only do the update on the last one, so partial RGB info isn't parsed.
                    Utility.SetNumericUpDownValue(redUpDown, value.R);
                    Utility.SetNumericUpDownValue(greenUpDown, value.G);
                    SetColorGradientValuesRgb(value.R, value.G, value.B);
                    SetColorGradientMinMaxColorsRgb(value.R, value.G, value.B);

                    ignore--;
                    Utility.SetNumericUpDownValue(blueUpDown, value.B);
                    Update();

                    if (hexBox.Text.Length == 6) // skip this step if the hexBox is being edited
                    {
                        string hexText = GetHexNumericUpDownValue(value.R, value.G, value.B);
                        hexBox.Text = hexText;
                    }

                    SyncHsvFromRgb(value);
                    colorDisplayWidget.RectangleColor = color;

                    colorDisplayWidget.Invalidate();
                }
            }
        }

        private static string GetHexNumericUpDownValue(int red, int green, int blue)
        {
            int newHexNumber = (red << 16) | (green << 8) | blue;
            string newHexText = System.Convert.ToString(newHexNumber, 16);

            while (newHexText.Length < 6)
            {
                newHexText = "0" + newHexText;
            }

            return newHexText.ToUpper();
        }

        /// <summary>
        /// Whenever a color is changed via RGB methods, call this and the HSV
        /// counterparts will be sync'd up.
        /// </summary>
        /// <param name="newColor">The RGB color that should be converted to HSV.</param>
        private void SyncHsvFromRgb(Color newColor)
        {
            if (ignore == 0)
            {
                ignore++;
                HsvColor hsvColor = HsvColor.FromColor(newColor);

                Utility.SetNumericUpDownValue(hueUpDown, hsvColor.Hue);
                Utility.SetNumericUpDownValue(saturationUpDown, hsvColor.Saturation);
                Utility.SetNumericUpDownValue(valueUpDown, hsvColor.Value);

                SetColorGradientValuesHsv(hsvColor.Hue, hsvColor.Saturation, hsvColor.Value);
                SetColorGradientMinMaxColorsHsv(hsvColor.Hue, hsvColor.Saturation, hsvColor.Value);

                colorWheel.HsvColor = hsvColor;
                ignore--;
            }
        }

        private void SetColorGradientValuesRgb(int r, int g, int b)
        {
            PushIgnoreChangedEvents();

            if (redGradientControl.Value != r)
            {
                redGradientControl.Value = r;
            }

            if (greenGradientControl.Value != g)
            {
                greenGradientControl.Value = g;
            }

            if (blueGradientControl.Value != b)
            {
                blueGradientControl.Value = b;
            }

            PopIgnoreChangedEvents();
        }

        private void SetColorGradientValuesHsv(int h, int s, int v)
        {
            PushIgnoreChangedEvents();

            if (((hueGradientControl.Value * 360) / 255) != h)
            {
                hueGradientControl.Value = (255 * h) / 360;
            }

            if (((saturationGradientControl.Value * 100) / 255) != s)
            {
                saturationGradientControl.Value = (255 * s) / 100;
            }

            if (((valueGradientControl.Value * 100) / 255) != v)
            {
                valueGradientControl.Value = (255 * v) / 100;
            }

            PopIgnoreChangedEvents();
        }

        /// <summary>
        /// Whenever a color is changed via HSV methods, call this and the RGB
        /// counterparts will be sync'd up.
        /// </summary>
        /// <param name="newColor">The HSV color that should be converted to RGB.</param>
        private void SyncRgbFromHsv(HsvColor newColor)
        {
            if (ignore == 0)
            {
                ignore++;
                RgbColor rgbColor = newColor.ToRgb();

                Utility.SetNumericUpDownValue(redUpDown, rgbColor.Red);
                Utility.SetNumericUpDownValue(greenUpDown, rgbColor.Green);
                Utility.SetNumericUpDownValue(blueUpDown, rgbColor.Blue);

                string hexText = GetHexNumericUpDownValue(rgbColor.Red, rgbColor.Green, rgbColor.Blue);
                hexBox.Text = hexText;

                SetColorGradientValuesRgb(rgbColor.Red, rgbColor.Green, rgbColor.Blue);
                SetColorGradientMinMaxColorsRgb(rgbColor.Red, rgbColor.Green, rgbColor.Blue);

                ignore--;
            }
        }

        public ColorPickerForm(string prompt)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            promptLbl.Text = prompt;

            redLabel.Text = "R:";
            greenLabel.Text = "G:";
            blueLabel.Text = "B:";

            hueLabel.Text = "H:";
            saturationLabel.Text = "S:";
            valueLabel.Text = "V:";

            rgbHeader.Text = "RGB";
            hexLabel.Text = "Hex:";
            hsvHeader.Text = "HSV";

            swatchControl.Colors = paletteColors;
            hexBox.Text = "000000";
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                    components = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            redUpDown = new System.Windows.Forms.NumericUpDown();
            greenUpDown = new System.Windows.Forms.NumericUpDown();
            blueUpDown = new System.Windows.Forms.NumericUpDown();
            redLabel = new System.Windows.Forms.Label();
            blueLabel = new System.Windows.Forms.Label();
            greenLabel = new System.Windows.Forms.Label();
            saturationLabel = new System.Windows.Forms.Label();
            valueLabel = new System.Windows.Forms.Label();
            hueLabel = new System.Windows.Forms.Label();
            valueUpDown = new System.Windows.Forms.NumericUpDown();
            saturationUpDown = new System.Windows.Forms.NumericUpDown();
            hueUpDown = new System.Windows.Forms.NumericUpDown();
            hexBox = new System.Windows.Forms.TextBox();
            hexLabel = new System.Windows.Forms.Label();
            okBtn = new System.Windows.Forms.Button();
            cancelBtn = new System.Windows.Forms.Button();
            blueGradientControl = new PaintDotNet.ColorGradientControl();
            greenGradientControl = new PaintDotNet.ColorGradientControl();
            redGradientControl = new PaintDotNet.ColorGradientControl();
            saturationGradientControl = new PaintDotNet.ColorGradientControl();
            hueGradientControl = new PaintDotNet.ColorGradientControl();
            colorWheel = new PaintDotNet.ColorWheel();
            hsvHeader = new PaintDotNet.HeaderLabel();
            rgbHeader = new PaintDotNet.HeaderLabel();
            valueGradientControl = new PaintDotNet.ColorGradientControl();
            colorDisplayWidget = new PaintDotNet.ColorRectangleControl();
            swatchHeader = new PaintDotNet.HeaderLabel();
            swatchControl = new PaintDotNet.SwatchControl();
            promptLbl = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(redUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(greenUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(blueUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(valueUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(saturationUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(hueUpDown)).BeginInit();
            SuspendLayout();
            //
            // redUpDown
            //
            redUpDown.Location = new System.Drawing.Point(317, 34);
            redUpDown.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            redUpDown.Name = "redUpDown";
            redUpDown.Size = new System.Drawing.Size(56, 20);
            redUpDown.TabIndex = 2;
            redUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            redUpDown.ValueChanged += new System.EventHandler(UpDown_ValueChanged);
            redUpDown.Enter += new System.EventHandler(UpDown_Enter);
            redUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(UpDown_KeyUp);
            redUpDown.Leave += new System.EventHandler(UpDown_Leave);
            //
            // greenUpDown
            //
            greenUpDown.Location = new System.Drawing.Point(317, 58);
            greenUpDown.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            greenUpDown.Name = "greenUpDown";
            greenUpDown.Size = new System.Drawing.Size(56, 20);
            greenUpDown.TabIndex = 3;
            greenUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            greenUpDown.ValueChanged += new System.EventHandler(UpDown_ValueChanged);
            greenUpDown.Enter += new System.EventHandler(UpDown_Enter);
            greenUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(UpDown_KeyUp);
            greenUpDown.Leave += new System.EventHandler(UpDown_Leave);
            //
            // blueUpDown
            //
            blueUpDown.Location = new System.Drawing.Point(317, 82);
            blueUpDown.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            blueUpDown.Name = "blueUpDown";
            blueUpDown.Size = new System.Drawing.Size(56, 20);
            blueUpDown.TabIndex = 4;
            blueUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            blueUpDown.ValueChanged += new System.EventHandler(UpDown_ValueChanged);
            blueUpDown.Enter += new System.EventHandler(UpDown_Enter);
            blueUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(UpDown_KeyUp);
            blueUpDown.Leave += new System.EventHandler(UpDown_Leave);
            //
            // redLabel
            //
            redLabel.AutoSize = true;
            redLabel.Location = new System.Drawing.Point(219, 38);
            redLabel.Name = "redLabel";
            redLabel.Size = new System.Drawing.Size(15, 13);
            redLabel.TabIndex = 7;
            redLabel.Text = "R";
            redLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // blueLabel
            //
            blueLabel.AutoSize = true;
            blueLabel.Location = new System.Drawing.Point(219, 86);
            blueLabel.Name = "blueLabel";
            blueLabel.Size = new System.Drawing.Size(14, 13);
            blueLabel.TabIndex = 8;
            blueLabel.Text = "B";
            blueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // greenLabel
            //
            greenLabel.AutoSize = true;
            greenLabel.Location = new System.Drawing.Point(219, 62);
            greenLabel.Name = "greenLabel";
            greenLabel.Size = new System.Drawing.Size(15, 13);
            greenLabel.TabIndex = 9;
            greenLabel.Text = "G";
            greenLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // saturationLabel
            //
            saturationLabel.AutoSize = true;
            saturationLabel.Location = new System.Drawing.Point(219, 174);
            saturationLabel.Name = "saturationLabel";
            saturationLabel.Size = new System.Drawing.Size(17, 13);
            saturationLabel.TabIndex = 16;
            saturationLabel.Text = "S:";
            saturationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // valueLabel
            //
            valueLabel.AutoSize = true;
            valueLabel.Location = new System.Drawing.Point(219, 198);
            valueLabel.Name = "valueLabel";
            valueLabel.Size = new System.Drawing.Size(17, 13);
            valueLabel.TabIndex = 15;
            valueLabel.Text = "V:";
            valueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // hueLabel
            //
            hueLabel.AutoSize = true;
            hueLabel.Location = new System.Drawing.Point(219, 150);
            hueLabel.Name = "hueLabel";
            hueLabel.Size = new System.Drawing.Size(18, 13);
            hueLabel.TabIndex = 14;
            hueLabel.Text = "H:";
            hueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // valueUpDown
            //
            valueUpDown.Location = new System.Drawing.Point(317, 194);
            valueUpDown.Name = "valueUpDown";
            valueUpDown.Size = new System.Drawing.Size(56, 20);
            valueUpDown.TabIndex = 8;
            valueUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            valueUpDown.ValueChanged += new System.EventHandler(UpDown_ValueChanged);
            valueUpDown.Enter += new System.EventHandler(UpDown_Enter);
            valueUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(UpDown_KeyUp);
            valueUpDown.Leave += new System.EventHandler(UpDown_Leave);
            //
            // saturationUpDown
            //
            saturationUpDown.Location = new System.Drawing.Point(317, 170);
            saturationUpDown.Name = "saturationUpDown";
            saturationUpDown.Size = new System.Drawing.Size(56, 20);
            saturationUpDown.TabIndex = 7;
            saturationUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            saturationUpDown.ValueChanged += new System.EventHandler(UpDown_ValueChanged);
            saturationUpDown.Enter += new System.EventHandler(UpDown_Enter);
            saturationUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(UpDown_KeyUp);
            saturationUpDown.Leave += new System.EventHandler(UpDown_Leave);
            //
            // hueUpDown
            //
            hueUpDown.Location = new System.Drawing.Point(317, 146);
            hueUpDown.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            hueUpDown.Name = "hueUpDown";
            hueUpDown.Size = new System.Drawing.Size(56, 20);
            hueUpDown.TabIndex = 6;
            hueUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            hueUpDown.ValueChanged += new System.EventHandler(UpDown_ValueChanged);
            hueUpDown.Enter += new System.EventHandler(UpDown_Enter);
            hueUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(UpDown_KeyUp);
            hueUpDown.Leave += new System.EventHandler(UpDown_Leave);
            //
            // hexBox
            //
            hexBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            hexBox.Location = new System.Drawing.Point(317, 106);
            hexBox.Name = "hexBox";
            hexBox.Size = new System.Drawing.Size(56, 20);
            hexBox.TabIndex = 5;
            hexBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            hexBox.TextChanged += new System.EventHandler(UpDown_ValueChanged);
            hexBox.Enter += new System.EventHandler(HexUpDown_Enter);
            hexBox.KeyDown += new System.Windows.Forms.KeyEventHandler(hexBox_KeyDown);
            hexBox.Leave += new System.EventHandler(HexUpDown_Leave);
            //
            // hexLabel
            //
            hexLabel.AutoSize = true;
            hexLabel.Location = new System.Drawing.Point(219, 109);
            hexLabel.Name = "hexLabel";
            hexLabel.Size = new System.Drawing.Size(26, 13);
            hexLabel.TabIndex = 13;
            hexLabel.Text = "Hex";
            hexLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // okBtn
            //
            okBtn.Location = new System.Drawing.Point(217, 252);
            okBtn.Name = "okBtn";
            okBtn.Size = new System.Drawing.Size(75, 23);
            okBtn.TabIndex = 40;
            okBtn.Text = "Ok";
            okBtn.UseVisualStyleBackColor = true;
            okBtn.Click += new System.EventHandler(okBtn_Click);
            //
            // cancelBtn
            //
            cancelBtn.Location = new System.Drawing.Point(298, 252);
            cancelBtn.Name = "cancelBtn";
            cancelBtn.Size = new System.Drawing.Size(75, 23);
            cancelBtn.TabIndex = 41;
            cancelBtn.Text = "Cancel";
            cancelBtn.UseVisualStyleBackColor = true;
            cancelBtn.Click += new System.EventHandler(cancelBtn_Click);
            //
            // blueGradientControl
            //
            blueGradientControl.Count = 1;
            blueGradientControl.CustomGradient = null;
            blueGradientControl.DrawFarNub = true;
            blueGradientControl.DrawNearNub = false;
            blueGradientControl.Location = new System.Drawing.Point(240, 83);
            blueGradientControl.MaxColor = System.Drawing.Color.White;
            blueGradientControl.MinColor = System.Drawing.Color.Black;
            blueGradientControl.Name = "blueGradientControl";
            blueGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            blueGradientControl.Size = new System.Drawing.Size(73, 19);
            blueGradientControl.TabIndex = 39;
            blueGradientControl.TabStop = false;
            blueGradientControl.Value = 0;
            blueGradientControl.ValueChanged += new System.EventHandler<PaintDotNet.IndexEventArgs>(RgbGradientControl_ValueChanged);
            //
            // greenGradientControl
            //
            greenGradientControl.Count = 1;
            greenGradientControl.CustomGradient = null;
            greenGradientControl.DrawFarNub = true;
            greenGradientControl.DrawNearNub = false;
            greenGradientControl.Location = new System.Drawing.Point(240, 59);
            greenGradientControl.MaxColor = System.Drawing.Color.White;
            greenGradientControl.MinColor = System.Drawing.Color.Black;
            greenGradientControl.Name = "greenGradientControl";
            greenGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            greenGradientControl.Size = new System.Drawing.Size(73, 19);
            greenGradientControl.TabIndex = 38;
            greenGradientControl.TabStop = false;
            greenGradientControl.Value = 0;
            greenGradientControl.ValueChanged += new System.EventHandler<PaintDotNet.IndexEventArgs>(RgbGradientControl_ValueChanged);
            //
            // redGradientControl
            //
            redGradientControl.Count = 1;
            redGradientControl.CustomGradient = null;
            redGradientControl.DrawFarNub = true;
            redGradientControl.DrawNearNub = false;
            redGradientControl.Location = new System.Drawing.Point(240, 35);
            redGradientControl.MaxColor = System.Drawing.Color.White;
            redGradientControl.MinColor = System.Drawing.Color.Black;
            redGradientControl.Name = "redGradientControl";
            redGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            redGradientControl.Size = new System.Drawing.Size(73, 19);
            redGradientControl.TabIndex = 37;
            redGradientControl.TabStop = false;
            redGradientControl.Value = 0;
            redGradientControl.ValueChanged += new System.EventHandler<PaintDotNet.IndexEventArgs>(RgbGradientControl_ValueChanged);
            //
            // saturationGradientControl
            //
            saturationGradientControl.Count = 1;
            saturationGradientControl.CustomGradient = null;
            saturationGradientControl.DrawFarNub = true;
            saturationGradientControl.DrawNearNub = false;
            saturationGradientControl.Location = new System.Drawing.Point(240, 171);
            saturationGradientControl.MaxColor = System.Drawing.Color.White;
            saturationGradientControl.MinColor = System.Drawing.Color.Black;
            saturationGradientControl.Name = "saturationGradientControl";
            saturationGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            saturationGradientControl.Size = new System.Drawing.Size(73, 19);
            saturationGradientControl.TabIndex = 35;
            saturationGradientControl.TabStop = false;
            saturationGradientControl.Value = 0;
            saturationGradientControl.ValueChanged += new System.EventHandler<PaintDotNet.IndexEventArgs>(HsvGradientControl_ValueChanged);
            //
            // hueGradientControl
            //
            hueGradientControl.Count = 1;
            hueGradientControl.CustomGradient = null;
            hueGradientControl.DrawFarNub = true;
            hueGradientControl.DrawNearNub = false;
            hueGradientControl.Location = new System.Drawing.Point(240, 147);
            hueGradientControl.MaxColor = System.Drawing.Color.White;
            hueGradientControl.MinColor = System.Drawing.Color.Black;
            hueGradientControl.Name = "hueGradientControl";
            hueGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            hueGradientControl.Size = new System.Drawing.Size(73, 19);
            hueGradientControl.TabIndex = 34;
            hueGradientControl.TabStop = false;
            hueGradientControl.Value = 0;
            hueGradientControl.ValueChanged += new System.EventHandler<PaintDotNet.IndexEventArgs>(HsvGradientControl_ValueChanged);
            //
            // colorWheel
            //
            colorWheel.Location = new System.Drawing.Point(53, 41);
            colorWheel.Name = "colorWheel";
            colorWheel.Size = new System.Drawing.Size(146, 147);
            colorWheel.TabIndex = 3;
            colorWheel.TabStop = false;
            colorWheel.ColorChanged += new System.EventHandler(ColorWheel_ColorChanged);
            //
            // hsvHeader
            //
            hsvHeader.ForeColor = System.Drawing.SystemColors.Highlight;
            hsvHeader.Location = new System.Drawing.Point(219, 130);
            hsvHeader.Name = "hsvHeader";
            hsvHeader.RightMargin = 0;
            hsvHeader.Size = new System.Drawing.Size(154, 14);
            hsvHeader.TabIndex = 28;
            hsvHeader.TabStop = false;
            //
            // rgbHeader
            //
            rgbHeader.ForeColor = System.Drawing.SystemColors.Highlight;
            rgbHeader.Location = new System.Drawing.Point(219, 18);
            rgbHeader.Name = "rgbHeader";
            rgbHeader.RightMargin = 0;
            rgbHeader.Size = new System.Drawing.Size(154, 14);
            rgbHeader.TabIndex = 27;
            rgbHeader.TabStop = false;
            //
            // valueGradientControl
            //
            valueGradientControl.Count = 1;
            valueGradientControl.CustomGradient = null;
            valueGradientControl.DrawFarNub = true;
            valueGradientControl.DrawNearNub = false;
            valueGradientControl.Location = new System.Drawing.Point(240, 195);
            valueGradientControl.MaxColor = System.Drawing.Color.White;
            valueGradientControl.MinColor = System.Drawing.Color.Black;
            valueGradientControl.Name = "valueGradientControl";
            valueGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            valueGradientControl.Size = new System.Drawing.Size(73, 19);
            valueGradientControl.TabIndex = 2;
            valueGradientControl.TabStop = false;
            valueGradientControl.Value = 0;
            valueGradientControl.ValueChanged += new System.EventHandler<PaintDotNet.IndexEventArgs>(HsvGradientControl_ValueChanged);
            //
            // colorDisplayWidget
            //
            colorDisplayWidget.Location = new System.Drawing.Point(6, 33);
            colorDisplayWidget.Name = "colorDisplayWidget";
            colorDisplayWidget.RectangleColor = System.Drawing.Color.Empty;
            colorDisplayWidget.Size = new System.Drawing.Size(42, 42);
            colorDisplayWidget.TabIndex = 32;
            //
            // swatchHeader
            //
            swatchHeader.ForeColor = System.Drawing.SystemColors.Highlight;
            swatchHeader.Location = new System.Drawing.Point(7, 194);
            swatchHeader.Name = "swatchHeader";
            swatchHeader.RightMargin = 0;
            swatchHeader.Size = new System.Drawing.Size(193, 14);
            swatchHeader.TabIndex = 30;
            swatchHeader.TabStop = false;
            //
            // swatchControl
            //
            swatchControl.BlinkHighlight = false;
            swatchControl.Colors = new System.Drawing.Color[0];
            swatchControl.Location = new System.Drawing.Point(7, 206);
            swatchControl.Name = "swatchControl";
            swatchControl.Size = new System.Drawing.Size(192, 74);
            swatchControl.TabIndex = 31;
            swatchControl.Text = "swatchControl1";
            swatchControl.ColorClicked += new System.EventHandler<PaintDotNet.IndexEventArgs>(swatchControl_ColorClicked);
            //
            // promptLbl
            //
            promptLbl.AutoSize = true;
            promptLbl.Location = new System.Drawing.Point(4, 9);
            promptLbl.Name = "promptLbl";
            promptLbl.Size = new System.Drawing.Size(81, 13);
            promptLbl.TabIndex = 42;
            promptLbl.Text = "Choose a color:";
            //
            // ColorPickerForm
            //
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(386, 284);
            Controls.Add(promptLbl);
            Controls.Add(cancelBtn);
            Controls.Add(okBtn);
            Controls.Add(valueLabel);
            Controls.Add(saturationLabel);
            Controls.Add(hueLabel);
            Controls.Add(greenLabel);
            Controls.Add(blueLabel);
            Controls.Add(redLabel);
            Controls.Add(hexLabel);
            Controls.Add(blueGradientControl);
            Controls.Add(greenGradientControl);
            Controls.Add(redGradientControl);
            Controls.Add(saturationGradientControl);
            Controls.Add(hueGradientControl);
            Controls.Add(colorWheel);
            Controls.Add(hsvHeader);
            Controls.Add(rgbHeader);
            Controls.Add(valueGradientControl);
            Controls.Add(blueUpDown);
            Controls.Add(greenUpDown);
            Controls.Add(redUpDown);
            Controls.Add(hexBox);
            Controls.Add(hueUpDown);
            Controls.Add(saturationUpDown);
            Controls.Add(valueUpDown);
            Controls.Add(colorDisplayWidget);
            Controls.Add(swatchHeader);
            Controls.Add(swatchControl);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ColorPickerForm";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Color Picker";
            ((System.ComponentModel.ISupportInitialize)(redUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(greenUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(blueUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(valueUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(saturationUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(hueUpDown)).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private void ColorWheel_ColorChanged(object sender, EventArgs e)
        {
            if (IgnoreChangedEvents)
            {
                return;
            }

            PushIgnoreChangedEvents();

            HsvColor hsvColor = colorWheel.HsvColor;
            RgbColor rgbColor = hsvColor.ToRgb();
            Color color = Color.FromArgb((byte)rgbColor.Red, (byte)rgbColor.Green, (byte)rgbColor.Blue);

            Utility.SetNumericUpDownValue(hueUpDown, hsvColor.Hue);
            Utility.SetNumericUpDownValue(saturationUpDown, hsvColor.Saturation);
            Utility.SetNumericUpDownValue(valueUpDown, hsvColor.Value);

            Utility.SetNumericUpDownValue(redUpDown, color.R);
            Utility.SetNumericUpDownValue(greenUpDown, color.G);
            Utility.SetNumericUpDownValue(blueUpDown, color.B);

            string hexText = GetHexNumericUpDownValue(color.R, color.G, color.B);
            hexBox.Text = hexText;

            SetColorGradientValuesHsv(hsvColor.Hue, hsvColor.Saturation, hsvColor.Value);
            SetColorGradientMinMaxColorsHsv(hsvColor.Hue, hsvColor.Saturation, hsvColor.Value);

            SetColorGradientValuesRgb(color.R, color.G, color.B);
            SetColorGradientMinMaxColorsRgb(color.R, color.G, color.B);

            Color = color;

            PopIgnoreChangedEvents();

            Update();
        }

        private void SetColorGradientMinMaxColorsHsv(int h, int s, int v)
        {
            Color[] hueColors = new Color[361];

            for (int newH = 0; newH <= 360; ++newH)
            {
                HsvColor hsv = new HsvColor(newH, 100, 100);
                hueColors[newH] = hsv.ToColor();
            }

            hueGradientControl.CustomGradient = hueColors;

            Color[] satColors = new Color[101];

            for (int newS = 0; newS <= 100; ++newS)
            {
                HsvColor hsv = new HsvColor(h, newS, v);
                satColors[newS] = hsv.ToColor();
            }

            saturationGradientControl.CustomGradient = satColors;

            valueGradientControl.MaxColor = new HsvColor(h, s, 100).ToColor();
            valueGradientControl.MinColor = new HsvColor(h, s, 0).ToColor();
        }

        private void SetColorGradientMinMaxColorsRgb(int r, int g, int b)
        {
            redGradientControl.MaxColor = Color.FromArgb(255, g, b);
            redGradientControl.MinColor = Color.FromArgb(0, g, b);
            greenGradientControl.MaxColor = Color.FromArgb(r, 255, b);
            greenGradientControl.MinColor = Color.FromArgb(r, 0, b);
            blueGradientControl.MaxColor = Color.FromArgb(r, g, 255);
            blueGradientControl.MinColor = Color.FromArgb(r, g, 0);
        }

        private void RgbGradientControl_ValueChanged(object sender, IndexEventArgs ce)
        {
            if (IgnoreChangedEvents)
            {
                return;
            }

            int red;
            if (sender == redGradientControl)
            {
                red = redGradientControl.Value;
            }
            else
            {
                red = (int)redUpDown.Value;
            }

            int green;
            if (sender == greenGradientControl)
            {
                green = greenGradientControl.Value;
            }
            else
            {
                green = (int)greenUpDown.Value;
            }

            int blue;
            if (sender == blueGradientControl)
            {
                blue = blueGradientControl.Value;
            }
            else
            {
                blue = (int)blueUpDown.Value;
            }

            Color rgbColor = Color.FromArgb(255, red, green, blue);
            HsvColor hsvColor = HsvColor.FromColor(rgbColor);

            PushIgnoreChangedEvents();
            Utility.SetNumericUpDownValue(hueUpDown, hsvColor.Hue);
            Utility.SetNumericUpDownValue(saturationUpDown, hsvColor.Saturation);
            Utility.SetNumericUpDownValue(valueUpDown, hsvColor.Value);

            Utility.SetNumericUpDownValue(redUpDown, rgbColor.R);
            Utility.SetNumericUpDownValue(greenUpDown, rgbColor.G);
            Utility.SetNumericUpDownValue(blueUpDown, rgbColor.B);
            PopIgnoreChangedEvents();

            string hexText = GetHexNumericUpDownValue(rgbColor.R, rgbColor.G, rgbColor.B);
            hexBox.Text = hexText;

            Color = rgbColor;

            Update();
        }

        private void HsvGradientControl_ValueChanged(object sender, IndexEventArgs e)
        {
            if (IgnoreChangedEvents)
            {
                return;
            }

            int hue;
            if (sender == hueGradientControl)
            {
                hue = (hueGradientControl.Value * 360) / 255;
            }
            else
            {
                hue = (int)hueUpDown.Value;
            }

            int saturation;
            if (sender == saturationGradientControl)
            {
                saturation = (saturationGradientControl.Value * 100) / 255;
            }
            else
            {
                saturation = (int)saturationUpDown.Value;
            }

            int value;
            if (sender == valueGradientControl)
            {
                value = (valueGradientControl.Value * 100) / 255;
            }
            else
            {
                value = (int)valueUpDown.Value;
            }

            HsvColor hsvColor = new HsvColor(hue, saturation, value);
            colorWheel.HsvColor = hsvColor;
            RgbColor rgbColor = hsvColor.ToRgb();
            Color color = Color.FromArgb((byte)rgbColor.Red, (byte)rgbColor.Green, (byte)rgbColor.Blue);

            Utility.SetNumericUpDownValue(hueUpDown, hsvColor.Hue);
            Utility.SetNumericUpDownValue(saturationUpDown, hsvColor.Saturation);
            Utility.SetNumericUpDownValue(valueUpDown, hsvColor.Value);

            Utility.SetNumericUpDownValue(redUpDown, rgbColor.Red);
            Utility.SetNumericUpDownValue(greenUpDown, rgbColor.Green);
            Utility.SetNumericUpDownValue(blueUpDown, rgbColor.Blue);

            string hexText = GetHexNumericUpDownValue(rgbColor.Red, rgbColor.Green, rgbColor.Blue);
            hexBox.Text = hexText;

            Color = color;

            Update();
        }

        private void UpDown_Enter(object sender, System.EventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;
            nud.Select(0, nud.Text.Length);
        }

        private void UpDown_Leave(object sender, System.EventArgs e)
        {
            UpDown_ValueChanged(sender, e);
        }

        private void HexUpDown_Enter(object sender, System.EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Select(0, tb.Text.Length);
        }

        private void HexUpDown_Leave(object sender, System.EventArgs e)
        {
            hexBox.Text = hexBox.Text.ToUpper();
            UpDown_ValueChanged(sender, e);
        }

        private void hexBox_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) || (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) && ModifierKeys != Keys.Shift)
            {
                e.Handled = true;
                e.SuppressKeyPress = false;
            }
            else if (e.KeyCode == Keys.A || e.KeyCode == Keys.B || e.KeyCode == Keys.C || e.KeyCode == Keys.D || e.KeyCode == Keys.E || e.KeyCode == Keys.F || e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                e.Handled = true;
                e.SuppressKeyPress = false;
            }
            else
            {
                e.Handled = false;
                e.SuppressKeyPress = true;
            }
        }

        private void UpDown_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;

            if (Utility.CheckNumericUpDown(nud))
            {
                UpDown_ValueChanged(sender, e);
            }
        }

        private void UpDown_ValueChanged(object sender, System.EventArgs e)
        {
            if (IgnoreChangedEvents)
            {
                return;
            }
            else
            {
                PushIgnoreChangedEvents();
                if (sender == redUpDown || sender == greenUpDown || sender == blueUpDown)
                {
                    string hexText = GetHexNumericUpDownValue((int)redUpDown.Value, (int)greenUpDown.Value, (int)blueUpDown.Value);
                    hexBox.Text = hexText;

                    Color rgbColor = Color.FromArgb((byte)redUpDown.Value, (byte)greenUpDown.Value, (byte)blueUpDown.Value);

                    SetColorGradientMinMaxColorsRgb(rgbColor.R, rgbColor.G, rgbColor.B);
                    SetColorGradientValuesRgb(rgbColor.R, rgbColor.G, rgbColor.B);

                    SyncHsvFromRgb(rgbColor);

                    Color = rgbColor;
                }
                else if (sender == hexBox)
                {
                    int hexInt = 0;

                    if (hexBox.Text.Length > 0)
                    {
                        try
                        {
                            hexInt = int.Parse(hexBox.Text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
                        }

                        // Needs to be changed so it reads what the RGB values were last
                        catch (FormatException)
                        {
                            hexInt = 0;
                            hexBox.Text = "";
                        }
                        catch (OverflowException)
                        {
                            hexInt = 16777215;
                            hexBox.Text = "FFFFFF";
                        }

                        if (!((hexInt <= 16777215) && (hexInt >= 0)))
                        {
                            hexInt = 16777215;
                            hexBox.Text = "FFFFFF";
                        }
                    }

                    int newRed = ((hexInt & 0xff0000) >> 16);
                    int newGreen = ((hexInt & 0x00ff00) >> 8);
                    int newBlue = (hexInt & 0x0000ff);

                    Utility.SetNumericUpDownValue(redUpDown, newRed);
                    Utility.SetNumericUpDownValue(greenUpDown, newGreen);
                    Utility.SetNumericUpDownValue(blueUpDown, newBlue);

                    SetColorGradientMinMaxColorsRgb(newRed, newGreen, newBlue);
                    SetColorGradientValuesRgb(newRed, newGreen, newBlue);

                    Color rgbColor = Color.FromArgb((byte)newRed, (byte)newGreen, (byte)newBlue);
                    SyncHsvFromRgb(rgbColor);
                    Color = rgbColor;
                }
                else if (sender == hueUpDown || sender == saturationUpDown || sender == valueUpDown)
                {
                    HsvColor oldHsvColor = colorWheel.HsvColor;
                    HsvColor newHsvColor = new HsvColor((int)hueUpDown.Value, (int)saturationUpDown.Value, (int)valueUpDown.Value);

                    if (oldHsvColor != newHsvColor)
                    {
                        colorWheel.HsvColor = newHsvColor;

                        SetColorGradientValuesHsv(newHsvColor.Hue, newHsvColor.Saturation, newHsvColor.Value);
                        SetColorGradientMinMaxColorsHsv(newHsvColor.Hue, newHsvColor.Saturation, newHsvColor.Value);

                        SyncRgbFromHsv(newHsvColor);
                        RgbColor rgbColor = newHsvColor.ToRgb();
                        Color = rgbColor.ToColor();
                    }
                }
                PopIgnoreChangedEvents();
            }
        }

        private void PushIgnoreChangedEvents()
        {
            ++ignoreChangedEvents;
        }

        private void PopIgnoreChangedEvents()
        {
            --ignoreChangedEvents;
        }

        private void swatchControl_ColorClicked(object sender, IndexEventArgs e)
        {
            Color = paletteColors[e.Index];
        }

        private void okBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
