#region Copyright Statement
// Copyright © 2009 Shumbi Ltd.
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Obany.Core;
using Obany.UI.Controls;
using System;

namespace ShumbiDiscover.Notes
{
    /// <summary>
    /// Class to show an annotation control
    /// </summary>
    public partial class AnnotationCanvas : UserControl
    {
        #region Delegates
        /// <summary>
        /// Event handler used for exiting annotation
        /// </summary>
        public delegate void ExitAnnotationEventHandler();
        /// <summary>
        /// Event handler used for saving annotation
        /// </summary>
        /// <param name="canvas">The canvas to render</param>
        /// <param name="mimeType">Type to save as</param>
        public delegate void SaveAnnotationEventHandler(Obany.Render.Objects.Canvas canvas, string mimeType);
        /// <summary>
        /// Event handler used for sharing annotation
        /// </summary>
        /// <param name="canvas">The canvas to render</param>
        /// <param name="provider">The provider to share with</param>
        public delegate void ShareAnnotationEventHandler(Obany.Render.Objects.Canvas canvas, string provider);
        #endregion

        #region Fields
        private Color _currentColour;
        private double _currentPenWidth;
        private double _currentPenHeight;
        private Stroke _currentStroke;

        private Size _canvasDimensions;

        private PenColour _blackPen;
        private PenColour _redPen;
        private PenColour _yellowPen;
        private bool _isHighlighter;
        private PenThickness _thickness1;
        private PenThickness _thickness3;
        private PenThickness _thickness10;

        private UIElementCollection _uiElements;

        private int _changeCount;
        private Point _scrollCentre;
        #endregion

        #region Events
        /// <summary>
        /// Event called when annotation is exited
        /// </summary>
        public event ExitAnnotationEventHandler ExitAnnotation;
        /// <summary>
        /// Event called when annotation is saved
        /// </summary>
        public event SaveAnnotationEventHandler SaveAnnotation;
        /// <summary>
        /// Event called when annotation is shared
        /// </summary>
        public event ShareAnnotationEventHandler ShareAnnotation;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public AnnotationCanvas()
        {
            InitializeComponent();

            //_scrollViewer.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xFF));
            //gridZoom.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00)); 

#if SILVERLIGHT
            _uiElements = _inkPresenter.Children;
            _inkPresenter.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
#else
            Canvas canvas = new Canvas();
            canvas.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
            _inkPresenter.Child = canvas;
            _uiElements = canvas.Children;
#endif


#if SILVERLIGHT
            _inkPresenter.Cursor = Cursors.Stylus;
#else
            _inkPresenter.Cursor = Cursors.Pen;
#endif

            CreatePenColours();
            CreatePenThickness();
            ClearContent();

            _isHighlighter = false;

            ExitAnnotation = null;
            SaveAnnotation = null;

            thicknessCombo.SelectedItem = _thickness1;

#if SILVERLIGHT
            _scrollViewer.MouseWheel += new MouseWheelEventHandler(_scrollViewer_MouseWheel);
            _scrollViewer.LayoutUpdated += new EventHandler(_scrollViewer_LayoutUpdated);
#else
            _scrollViewer.PreviewMouseWheel += new MouseWheelEventHandler(_scrollViewer_MouseWheel);
            _scrollViewer.ScrollChanged += new ScrollChangedEventHandler(_scrollViewer_ScrollChanged);
#endif

            SetupCanvas(1024, 768);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Set the title
        /// </summary>
        public string Title
        {
            set
            {
                labelTitle.Text = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Hook the global hid events
        /// </summary>
        /// <param name="hook"></param>
        public void HookHidEvents(bool hook)
        {
#if !SILVERLIGHT
            if (hook)
            {
                Obany.Hid.Tablet.TabletManager.HidFlick += new Obany.Hid.Tablet.TabletManager.HidFlickEventHandler(TabletManager_HidFlick);
                Obany.Hid.Tablet.TabletManager.HidScroll += new Obany.Hid.Tablet.TabletManager.HidScrollEventHandler(TabletManager_HidScroll);
            }
            else
            {
                Obany.Hid.Tablet.TabletManager.HidFlick -= new Obany.Hid.Tablet.TabletManager.HidFlickEventHandler(TabletManager_HidFlick);
                Obany.Hid.Tablet.TabletManager.HidScroll -= new Obany.Hid.Tablet.TabletManager.HidScrollEventHandler(TabletManager_HidScroll);
            }
#endif
        }
        #endregion

        #region Control Event Handlers
        void _inkPresenter_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _inkPresenter.CaptureMouse();
#if SILVERLIGHT
            _currentStroke = new Stroke();
            _currentStroke.StylusPoints.Add(e.StylusDevice.GetStylusPoints(_inkPresenter));
#else
            _currentStroke = new Stroke(new StylusPointCollection(new List<Point>() { e.GetPosition(_inkPresenter) }));
#endif
            _currentStroke.DrawingAttributes.Color = _currentColour;
            _currentStroke.DrawingAttributes.Width = _currentPenWidth;
            _currentStroke.DrawingAttributes.Height = _currentPenHeight;

            _inkPresenter.Strokes.Add(_currentStroke);
            _changeCount++;
        }

        void _inkPresenter_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentStroke != null)
            {
#if SILVERLIGHT
                _currentStroke.StylusPoints.Add(e.StylusDevice.GetStylusPoints(_inkPresenter));
#else
                _currentStroke.StylusPoints.Add(new StylusPointCollection(new List<Point>() { e.GetPosition(_inkPresenter) }));
#endif
            }
        }

        void _inkPresenter_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _currentStroke = null;
            _inkPresenter.ReleaseMouseCapture();
        }

        private void buttonBallpoint_Click(object sender, RoutedEventArgs e)
        {
            _isHighlighter = false;
#if SILVERLIGHT
            _inkPresenter.Cursor = Cursors.Stylus;
#else
            _inkPresenter.Cursor = Cursors.Pen;
#endif
            colourCombo.SelectedItem = _blackPen;
            thicknessCombo.SelectedItem = _thickness1;
        }

        private void buttonFeltTip_Click(object sender, RoutedEventArgs e)
        {
            _isHighlighter = false;
#if SILVERLIGHT
            _inkPresenter.Cursor = Cursors.Stylus;
#else
            _inkPresenter.Cursor = Cursors.Pen;
#endif
            colourCombo.SelectedItem = _redPen;
            thicknessCombo.SelectedItem = _thickness3;
        }

        private void buttonHighlighter_Click(object sender, RoutedEventArgs e)
        {
            _isHighlighter = true;
#if SILVERLIGHT
            _inkPresenter.Cursor = Cursors.Stylus;
#else
            _inkPresenter.Cursor = Cursors.Pen;
#endif
            colourCombo.SelectedItem = _yellowPen;
            thicknessCombo.SelectedItem = _thickness10;
        }

        private void buttonDeleteAllInk_Click(object sender, RoutedEventArgs e)
        {
            _inkPresenter.Strokes.Clear();
            _changeCount = 0;
        }

        private void buttonDeleteLastInk_Click(object sender, RoutedEventArgs e)
        {
            if (_inkPresenter.Strokes.Count > 0)
            {
                _inkPresenter.Strokes.RemoveAt(_inkPresenter.Strokes.Count - 1);

                if (_inkPresenter.Strokes.Count == 0)
                {
                    _changeCount = 0;
                }
            }
        }


        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            if (ExitAnnotation != null)
            {
                if (_changeCount > 0)
                {
                    DialogPanel.ShowQuestionBox(CultureHelper.GetString(Properties.Resources.ResourceManager, "CHANGESWILLBEDISCARDED") +
                                                CultureHelper.GetString(Properties.Resources.ResourceManager, "CONTINUEANYWAY")
                                                , DialogButtons.Yes | DialogButtons.No,
                        delegate(DialogResult dialogResult)
                        {
                            if (dialogResult == DialogResult.Yes)
                            {
                                ExitAnnotation();
                            }
                        }
                    );
                }
                else
                {
                    ExitAnnotation();
                }
            }
        }

        private void buttonSaveJpg_Click(object sender, RoutedEventArgs e)
        {
            if (SaveAnnotation != null)
            {
                SaveAnnotation(CreateCanvas(), "image/jpeg");
            }
        }

        private void buttonSaveXps_Click(object sender, RoutedEventArgs e)
        {
            if (SaveAnnotation != null)
            {
                SaveAnnotation(CreateCanvas(), "application/vnd.ms-xpsdocument");
            }
        }

        private void buttonSavePdf_Click(object sender, RoutedEventArgs e)
        {
            if (SaveAnnotation != null)
            {
                SaveAnnotation(CreateCanvas(), "application/pdf");
            }
        }

        private void buttonShareScribd_Click(object sender, RoutedEventArgs e)
        {
            if (ShareAnnotation != null)
            {
                ShareAnnotation(CreateCanvas(), "Scribd");
            }
        }

        private void colourCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (colourCombo.SelectedItem != null)
            {
                Color newColour;

                if (colourCombo.SelectedItem is ComboBoxItem)
                {
                    newColour = ((colourCombo.SelectedItem as ComboBoxItem).Content as PenColour).Brush.Color;
                }
                else
                {
                    newColour = (colourCombo.SelectedItem as PenColour).Brush.Color;
                }

                if (!_isHighlighter)
                {
                    _currentColour = newColour;
                }
                else
                {
                    _currentColour = Color.FromArgb(0xA0, newColour.R, newColour.G, newColour.B);
                }
            }

        }

        private void thicknessCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (thicknessCombo.SelectedItem != null)
            {
                double newThickness;

                if (colourCombo.SelectedItem is ComboBoxItem)
                {
                    newThickness = ((thicknessCombo.SelectedItem as ComboBoxItem).Content as PenThickness).Thickness;
                }
                else
                {
                    newThickness = (thicknessCombo.SelectedItem as PenThickness).Thickness;
                }

                if (!_isHighlighter)
                {
                    _currentPenHeight = newThickness;
                    _currentPenWidth = newThickness;
                }
                else
                {
                    _currentPenHeight = newThickness;
                    _currentPenWidth = newThickness / 5;
                }
            }

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Localise the control
        /// </summary>
        public void Localise()
        {
            labelExitButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "CLOSE");

            labelSavePdfButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SAVEAS").Replace("%1", "Pdf");
            labelSaveXpsButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SAVEAS").Replace("%1", "Xps");
            labelSaveJpgButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SAVEAS").Replace("%1", "Jpg");

            labelShareScribdButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SENDTO").Replace("%1", "Scribd");

            labelBallpointButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "INKBALLPOINT");
            labelFeltTipButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "INKFELTTIP");
            labelHighlighterButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "INKHIGHLIGHTER");
            labelDeleteLastInkButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "INKDELETE");
            labelDeleteAllInkButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "INKDELETEALL");
            labelThicknessCombo.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PENSIZE");
            labelColourCombo.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PENCOLOUR");

            labelZoom.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "ZOOM");

            CreatePenColours();
        }

        /// <summary>
        /// Reset the change count
        /// </summary>
        public void ResetChanges()
        {
            _changeCount = 0;
        }

        /// <summary>
        /// Clear the content of the annotation viewer
        /// </summary>
        public void ClearContent()
        {
            HookHidEvents(false);

            foreach (UIElement uiElement in _uiElements)
            {
                Image image = uiElement as Image;

                if (image != null)
                {
                    object[] data = (object[])image.Tag;

                    if (data != null)
                    {
                        if (data.Length == 3)
                        {
                            System.IO.MemoryStream ms = data[0] as System.IO.MemoryStream;

                            if (ms != null)
                            {
                                ms.Dispose();
                            }
                        }
                    }
                }
            }
            _uiElements.Clear();
            _inkPresenter.Strokes.Clear();
            _changeCount = 0;
        }

        /// <summary>
        /// Setup the canvas
        /// </summary>
        /// <param name="canvasWidth"></param>
        /// <param name="canvasHeight"></param>
        public void SetupCanvas(int canvasWidth, int canvasHeight)
        {
            ClearContent();

            HookHidEvents(true);

            _canvasDimensions = new Size(canvasWidth, canvasHeight);

            _inkPresenter.Width = _canvasDimensions.Width;
            _inkPresenter.Height = _canvasDimensions.Height;

            RectangleGeometry rg = new RectangleGeometry();
            rg.Rect = new Rect(0, 0, _canvasDimensions.Width, _canvasDimensions.Height);

            _inkPresenter.Clip = rg;

            gridZoom.Width = _canvasDimensions.Width;
            gridZoom.Height = _canvasDimensions.Height;

            _scrollCentre = new Point(0.5, 0);

            sliderZoom.Minimum = 0.1;
            sliderZoom.Maximum = 3;
            sliderZoom.SmallChange = (sliderZoom.Maximum - sliderZoom.Minimum) / 100;
            sliderZoom.LargeChange = (sliderZoom.Maximum - sliderZoom.Minimum) / 10;
            sliderZoom.Value = 1;

            SetZoom(sliderZoom.Value);
        }

        private void SetZoom(double zoomValue)
        {
            ScaleTransform st = new ScaleTransform();
            st.ScaleX = zoomValue;
            st.ScaleY = zoomValue;
            _inkPresenter.RenderTransform = st;

            // Adjust the size of the zoom grid so that the scrollviewer redraws the scroll bars
            gridZoom.Width = _canvasDimensions.Width * zoomValue;
            gridZoom.Height = _canvasDimensions.Height * zoomValue;

            double horizontalOffset;
            double verticalOffset;

            if (_scrollViewer.ViewportWidth > gridZoom.Width)
            {
                horizontalOffset = 0;
            }
            else
            {
                horizontalOffset = (_scrollCentre.X * gridZoom.Width) - (_scrollViewer.ViewportWidth / 2);
            }
            if (_scrollViewer.ViewportHeight > gridZoom.Height)
            {
                verticalOffset = 0;
            }
            else
            {
                verticalOffset = (_scrollCentre.Y * gridZoom.Height) - (_scrollViewer.ViewportHeight / 2);
            }

            _scrollViewer.ScrollToHorizontalOffset(horizontalOffset);
            _scrollViewer.ScrollToVerticalOffset(verticalOffset);
        }

        /// <summary>
        /// Add text to canvas
        /// </summary>
        /// <param name="text">The text</param>
        /// <param name="fontSize">The font size</param>
        /// <param name="fontName">The font name</param>
        /// <param name="left">The left position</param>
        /// <param name="top">The top position</param>
        public void AddText(string text, int fontSize, string fontName, int left, int top)
        {
            TextBlock tb = new TextBlock();
            tb.Text = text;
            tb.FontSize = fontSize;
            tb.FontFamily = new FontFamily(fontName);
            tb.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00));

            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, top);

            _uiElements.Add(tb);
        }

        /// <summary>
        /// Add a rectangle the the canvas
        /// </summary>
        /// <param name="colour">The colour of the rectangle</param>
        /// <param name="width">The width of the rectangle</param>
        /// <param name="height">The height of the rectangle</param>
        /// <param name="left">The left position</param>
        /// <param name="top">The top position</param>
        public void AddRectangle(Color colour, int width, int height, int left, int top)
        {
            System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();

            rect.Height = height;
            rect.Width = width;

            rect.Fill = new SolidColorBrush(colour);

            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);

            _uiElements.Add(rect);
        }

        /// <summary>
        /// Add a bitmap the the canvas
        /// </summary>
        /// <param name="imageData">The data for the bitmap</param>
        /// <param name="imageWidth">The width of the bitmap</param>
        /// <param name="imageHeight">The height of the bitmap</param>
        /// <param name="mimeType">The mime type of the image</param>
        /// <param name="resourceId">Resource id for the data</param>
        /// <param name="left">The left position</param>
        /// <param name="top">The top position</param>
        public void AddBitmapImage(byte[] imageData, int imageWidth, int imageHeight, string mimeType, string resourceId, int left, int top)
        {
            System.IO.MemoryStream bitmapStream = new System.IO.MemoryStream(imageData);

            Image image = new Image();
            image.Tag = new object[] { bitmapStream, resourceId, mimeType };
            image.Width = imageWidth;
            image.Height = imageHeight;

            BitmapImage bitmapImage = new BitmapImage();

#if SILVERLIGHT
            bitmapImage.SetSource(bitmapStream);
#else
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = bitmapStream;
            bitmapImage.EndInit();

#endif

            image.Source = bitmapImage;

            Canvas.SetLeft(image, left);
            Canvas.SetTop(image, top);

            _uiElements.Add(image);
        }
        #endregion

        #region Private Methods
        private Obany.Render.Objects.Canvas CreateCanvas()
        {
            Obany.Render.Objects.Canvas canvas = new Obany.Render.Objects.Canvas();

            canvas.Width = _inkPresenter.Width;
            canvas.Height = _inkPresenter.Height;
            canvas.Colour = "#FFFFFFFF";
            canvas.Children = new List<Obany.Render.Objects.BaseObject>();
            canvas.Strokes = new List<Obany.Render.Objects.Stroke>();

            foreach (UIElement uiElement in _uiElements)
            {
                TextBlock textBlock = uiElement as TextBlock;

                if (textBlock != null)
                {
                    canvas.Children.Add(new Obany.Render.Objects.TextBlock(Canvas.GetLeft(uiElement), Canvas.GetTop(uiElement), double.NaN, double.NaN, Canvas.GetZIndex(uiElement), textBlock.Text, textBlock.FontFamily.Source, textBlock.FontSize, (textBlock.Foreground as SolidColorBrush).Color.ToString()));
                }
                else
                {
                    Rectangle rectangle = uiElement as Rectangle;

                    if (rectangle != null)
                    {
                        canvas.Children.Add(new Obany.Render.Objects.Rectangle(Canvas.GetLeft(uiElement), Canvas.GetTop(uiElement), rectangle.Width, rectangle.Height, Canvas.GetZIndex(uiElement), (rectangle.Fill as SolidColorBrush).Color.ToString()));
                    }
                    else
                    {
                        Image image = uiElement as Image;

                        if (image != null)
                        {
                            string resourceId = null;
                            byte[] imageData = null;
                            string mimeType = "";

                            object[] data = (object[])image.Tag;

                            if (data != null)
                            {
                                if (data.Length == 3)
                                {
                                    resourceId = (string)data[1];
                                    mimeType = (string)data[2];
                                    if (string.IsNullOrEmpty(resourceId))
                                    {
                                        System.IO.MemoryStream ms = (System.IO.MemoryStream)data[0];
                                        if (ms != null)
                                        {
                                            imageData = ms.ToArray();
                                        }
                                    }
                                }
                            }

                            canvas.Children.Add(new Obany.Render.Objects.Image(Canvas.GetLeft(uiElement), Canvas.GetTop(uiElement), image.Width, image.Height, Canvas.GetZIndex(uiElement), imageData, mimeType, "", resourceId));
                        }
                    }
                }
            }

            foreach (Stroke stroke in _inkPresenter.Strokes)
            {
                Obany.Render.Objects.Stroke canvasStroke = new Obany.Render.Objects.Stroke();
                canvasStroke.Width = stroke.DrawingAttributes.Width;
                canvasStroke.Height = stroke.DrawingAttributes.Height;
                canvasStroke.Colour = stroke.DrawingAttributes.Color.ToString();
                canvasStroke.Points = new List<Obany.Render.Objects.StrokePoint>();

                foreach (StylusPoint stylusPoint in stroke.StylusPoints)
                {
                    canvasStroke.Points.Add(new Obany.Render.Objects.StrokePoint(stylusPoint.X, stylusPoint.Y));
                }

                canvas.Strokes.Add(canvasStroke);
            }

            return (canvas);
        }

        private void CreatePenColours()
        {
            List<PenColour> penColours = new List<PenColour>();

            _blackPen = new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORBLACK"), 0xFF, 0x00, 0x00, 0x00);

            penColours.Add(_blackPen);
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORBROWN"), 0xFF, 0x99, 0x33, 0x00));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLOROLIVEGREEN"), 0xFF, 0x33, 0x33, 0x00));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORDARKGREEN"), 0xFF, 0x00, 0x33, 0x00));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORDARKTEAL"), 0xFF, 0x00, 0x33, 0x66));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORDARKBLUE"), 0xFF, 0x00, 0x00, 0x80));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORINDIGO"), 0xFF, 0x33, 0x33, 0x99));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORGRAY80"), 0xFF, 0xA0, 0xA0, 0xA0));

            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORDARKRED"), 0xFF, 0x80, 0x00, 0x00));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORORANGE"), 0xFF, 0xFF, 0x66, 0x00));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORDARKYELLOW"), 0xFF, 0x80, 0x80, 0x00));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORGREEN"), 0xFF, 0x00, 0x80, 0x00));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORTEAL"), 0xFF, 0x00, 0x80, 0x80));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORBLUE"), 0xFF, 0x00, 0x00, 0xFF));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORBLUEGRAY"), 0xFF, 0x66, 0x66, 0x99));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORGRAY50"), 0xFF, 0x80, 0x80, 0x80));

            _redPen = new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORRED"), 0xFF, 0xFF, 0x00, 0x00);
            penColours.Add(_redPen);
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORLIGHTORANGE"), 0xFF, 0xFF, 0xE7, 0xA2));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORLIME"), 0xFF, 0x99, 0xCC, 0x00));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORSEAGREEN"), 0xFF, 0x33, 0x99, 0x66));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORAQUA"), 0xFF, 0x33, 0xCC, 0xCC));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORLIGHTBLUE"), 0xFF, 0x33, 0x66, 0xFF));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORVIOLET"), 0xFF, 0x80, 0x00, 0x80));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORGRAY40"), 0xFF, 0x96, 0x96, 0x96));

            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORPINK"), 0xFF, 0xFF, 0x00, 0xFF));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORGOLD"), 0xFF, 0xFF, 0xCC, 0x00));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORYELLOW"), 0xFF, 0xFF, 0xFF, 0x00));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORBRIGHTGREEN"), 0xFF, 0x00, 0xFF, 0x00));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORTURQUOISE"), 0xFF, 0x00, 0xFF, 0xFF));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORSKYBLUE"), 0xFF, 0x00, 0xCC, 0xFF));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORPLUM"), 0xFF, 0x99, 0x33, 0x66));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORGRAY25"), 0xFF, 0xA0, 0xA0, 0xA0));

            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORROSE"), 0xFF, 0xFF, 0x99, 0xCC));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORTAN"), 0xFF, 0xFF, 0xCC, 0x99));
            _yellowPen = new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORLIGHTYELLOW"), 0xFF, 0xFF, 0xFF, 0x99);
            penColours.Add(_yellowPen);
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORLIGHTGREEN"), 0xFF, 0xCC, 0xFF, 0xCC));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORLIGHTTURQUOISE"), 0xFF, 0xCC, 0xFF, 0xFF));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORPALEBLUE"), 0xFF, 0x99, 0xCC, 0xFF));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORLAVENDER"), 0xFF, 0xCC, 0x99, 0xFF));
            penColours.Add(new PenColour(CultureHelper.GetString(Properties.Resources.ResourceManager, "COLORWHITE"), 0xFF, 0xFF, 0xFF, 0xFF));

            colourCombo.ItemsSource = penColours;
            colourCombo.SelectedItem = _blackPen;
        }

        private void CreatePenThickness()
        {
            List<PenThickness> penThickness = new List<PenThickness>();

            penThickness.Add(new PenThickness("0.5px", 0.5));
            _thickness1 = new PenThickness("1px", 1);
            penThickness.Add(_thickness1);
            penThickness.Add(new PenThickness("2px", 2));
            _thickness3 = new PenThickness("3px", 3);
            penThickness.Add(_thickness3);
            penThickness.Add(new PenThickness("5px", 5));
            penThickness.Add(new PenThickness("7px", 7));
            _thickness10 = new PenThickness("10px", 10);
            penThickness.Add(_thickness10);
            penThickness.Add(new PenThickness("15px", 15));
            penThickness.Add(new PenThickness("20px", 20));
            penThickness.Add(new PenThickness("25px", 25));

            thicknessCombo.ItemsSource = penThickness;
        }
        #endregion

        #region Control Events

        void sliderZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetZoom(sliderZoom.Value);
            textZoom.Text = ((int)Math.Round(sliderZoom.Value * 100.0)).ToString() + "%";
        }

        void _scrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((System.Windows.Input.Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    sliderZoom.Value += 0.1;
                }
                else
                {
                    sliderZoom.Value -= 0.1;
                }

                e.Handled = true;
            }
#if SILVERLIGHT
            else
            {
                double newpos = _scrollViewer.VerticalOffset - e.Delta;
                if (newpos < 0)
                {
                    newpos = 0;
                }
                else if (newpos > _scrollViewer.ViewportHeight)
                {
                    newpos = _scrollViewer.ViewportHeight;
                }
                _scrollViewer.ScrollToVerticalOffset(newpos);

                e.Handled = true;
            }
#endif
        }

#if SILVERLIGHT
        void _scrollViewer_LayoutUpdated(object sender, EventArgs e)
#else
        void _scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
#endif

        {
            double xPos;
            double yPos;

            if (_scrollViewer.ViewportWidth > _scrollViewer.ExtentWidth)
            {
                xPos = 0.5;
            }
            else
            {
                xPos = (_scrollViewer.HorizontalOffset + (_scrollViewer.ViewportWidth / 2)) / _scrollViewer.ExtentWidth;
            }

            if (_scrollViewer.ViewportHeight > _scrollViewer.ExtentHeight)
            {
                yPos = 0.5;
            }
            else
            {
                yPos = (_scrollViewer.VerticalOffset + (_scrollViewer.ViewportHeight / 2)) / _scrollViewer.ExtentHeight;
            }
            _scrollCentre = new Point(xPos, yPos);
        }
        #endregion

        #region Flicks
#if !SILVERLIGHT
        void TabletManager_HidFlick(Obany.Hid.Tablet.Flick flick)
        {
            if (flick.FlickData.ActionCommandCode == Obany.Hid.Tablet.Interop.FLICKACTION_COMMANDCODE.FLICKACTION_COMMANDCODE_APPCOMMAND)
            {
            }
        }
#endif
        #endregion

        #region Scroll
#if !SILVERLIGHT
        void TabletManager_HidScroll(bool isHorizontal, Obany.Hid.Tablet.Interop.SCROLLBARCOMMAND scrollBarCommand)
        {
            if (isHorizontal)
            {
                if (scrollBarCommand == Obany.Hid.Tablet.Interop.SCROLLBARCOMMAND.SB_LINELEFT)
                {
                    _scrollViewer.LineLeft();
                }
                else if (scrollBarCommand == Obany.Hid.Tablet.Interop.SCROLLBARCOMMAND.SB_LINERIGHT)
                {
                    _scrollViewer.LineRight();
                }
            }
        }
#endif
        #endregion

    }
}
