#region Copyright Statement
// Copyright © 2009 Shumbi Ltd.
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Obany.Core;
using Obany.UI;
using Obany.UI.Controls;
using ShumbiDiscover.Data;

namespace ShumbiDiscover.Controls.Dialogs
{
    /// <summary>
    /// Class to display the favourites dialog
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public partial class Favourites : UserControl, ILocalisable
    {
        #region Delegates
        /// <summary>
        /// Event handler used when a favourite is opened
        /// </summary>
        /// <param name="favourite">The favourite to open</param>
        public delegate void OpenFavouriteEventHandler(Favourite favourite);
        #endregion

        #region Fields
        private FavouriteFolder _rootFolder;
        private object _dragObject;
        #endregion

        #region Events
        /// <summary>
        /// Event handler called when a favourite is opened
        /// </summary>
        public event OpenFavouriteEventHandler OpenFavourite;
        #endregion

        #region Public Events
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public Favourites()
        {
            InitializeComponent();

            UpdateButtonStates();

            treeView.StartDrag += new DragDropTreeView.StartDragEventHandler(treeView_StartDrag);
            treeView.MoveToParent += new DragDropTreeView.MoveToParentEventHandler(treeView_MoveToParent);

            // Since HierarchicalDataTemplate is in a different namespace in WPF and silverlight
            // we created the wpf version using a factory, we also do this because an exception
            // is thrown by wpf if we try and pass it the DragDropTreeViewItemTemplate

#if SILVERLIGHT
            treeView.ItemTemplate = Resources["FavouritesHierarchicalDataTemplate"] as DragDropTreeViewItemTemplate;
#else
            HierarchicalDataTemplate hdt = new HierarchicalDataTemplate();
            hdt.ItemsSource = new System.Windows.Data.Binding("Children");

            FrameworkElementFactory contentControlFactory = new FrameworkElementFactory(typeof(ContentControl));
            contentControlFactory.AddHandler(ContentControl.MouseDoubleClickEvent, new MouseButtonEventHandler(treeViewItem_MouseDoubleClick));

            FrameworkElementFactory gridFactory = new FrameworkElementFactory(typeof(Grid));

            FrameworkElementFactory columnDefinition1Factory = new FrameworkElementFactory(typeof(ColumnDefinition));
            FrameworkElementFactory columnDefinition2Factory = new FrameworkElementFactory(typeof(ColumnDefinition));
            FrameworkElementFactory columnDefinition3Factory = new FrameworkElementFactory(typeof(ColumnDefinition));

            columnDefinition1Factory.SetValue(ColumnDefinition.WidthProperty, new GridLength(32));
            columnDefinition2Factory.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            columnDefinition3Factory.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));

            contentControlFactory.AppendChild(gridFactory);

            gridFactory.AppendChild(columnDefinition1Factory);
            gridFactory.AppendChild(columnDefinition2Factory);
            gridFactory.AppendChild(columnDefinition3Factory);

            FrameworkElementFactory imageFactory = new FrameworkElementFactory(typeof(Image));
            imageFactory.SetValue(Image.WidthProperty, 32.0);
            imageFactory.SetValue(Image.HeightProperty, 32.0);

            System.Windows.Data.Binding sourceBinding = new System.Windows.Data.Binding();
            sourceBinding.Converter = Resources["FavouriteIconConverter"] as System.Windows.Data.IValueConverter;
            imageFactory.SetBinding(Image.SourceProperty, sourceBinding);

            FrameworkElementFactory textBlock1 = new FrameworkElementFactory(typeof(TextBlock));
            textBlock1.SetValue(TextBlock.ForegroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)));
            textBlock1.SetValue(Grid.ColumnProperty, 1);
            textBlock1.SetValue(TextBlock.MarginProperty, new Thickness(5, 0, 5, 0));
            textBlock1.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            textBlock1.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));

            FrameworkElementFactory textBlock2 = new FrameworkElementFactory(typeof(TextBlock));
            textBlock2.SetValue(TextBlock.ForegroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)));
            textBlock2.SetValue(Grid.ColumnProperty, 2);
            textBlock2.SetValue(TextBlock.MarginProperty, new Thickness(5, 0, 5, 0));
            textBlock2.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

            System.Windows.Data.Binding informationBinding = new System.Windows.Data.Binding();
            informationBinding.Converter = Resources["FavouriteNameConverter"] as System.Windows.Data.IValueConverter;
            textBlock2.SetBinding(TextBlock.TextProperty, informationBinding);

            gridFactory.AppendChild(imageFactory);
            gridFactory.AppendChild(textBlock1);
            gridFactory.AppendChild(textBlock2);

            hdt.VisualTree = contentControlFactory;

            treeView.ItemTemplate = hdt;
#endif
        }

        #endregion

        #region Properties
        /// <summary>
        /// Get or set the root favourites
        /// </summary>
        public FavouriteFolder RootFavourites
        {
            get
            {
                return (treeView.ItemsSource as FavouriteFolder);
            }
            set
            {
                _rootFolder = value;

                treeView.ItemsSource = value.Children;

                UpdateButtonStates();
            }
        }
        #endregion

        #region Event Handlers
        #endregion

        #region ILocalisable Members
        /// <summary>
        /// Localise the object
        /// </summary>
        public void Localise()
        {
            labelFavouritesNewFolder.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "FAVOURITESNEWFOLDER");
            labelFavouritesRename.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "FAVOURITESRENAME");
            labelFavouritesOpen.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "FAVOURITESOPEN");
            labelFavouritesDelete.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "FAVOURITESDELETE");
        }
        #endregion

        #region Control Event Handlers
        private void buttonFavouritesNewFolder_Click(object sender, RoutedEventArgs e)
        {
            FavouriteFolder newFolder = new FavouriteFolder();
            newFolder.Name = CultureHelper.GetString(Properties.Resources.ResourceManager, "FAVOURITESNEWFOLDER");

            Dialogs.FavouriteName dialog = new FavouriteName();

            dialog.ItemName = newFolder.Name;

            DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "PROPERTIES", dialog, "textFavouriteName", DialogButtons.Ok | DialogButtons.Cancel, 
                delegate(DialogResult dialogResult)
                {
                    if (dialogResult == DialogResult.Ok)
                    {
                        newFolder.Name = dialog.ItemName;

                        if (string.IsNullOrEmpty(newFolder.Name))
                        {
                            newFolder.Name = CultureHelper.GetString(Properties.Resources.ResourceManager, "FAVOURITESNEWFOLDER");
                        }

                        FavouriteFolder parent = treeView.SelectedItem as FavouriteFolder;

                        if (parent == null)
                        {
                            parent = FindParent(_rootFolder, treeView.SelectedItem as FavouriteBase);
                        }

                        if (parent == null)
                        {
                            parent = _rootFolder;
                        }

                        if (parent != null)
                        {
                            int foundIdx = -1;

                            for (int i = 0; i < parent.Children.Count && foundIdx == -1; i++)
                            {
                                if (parent.Children[i] is Favourite)
                                {
                                    foundIdx = i;
                                }
                            }

                            if (foundIdx == -1)
                            {
                                foundIdx = parent.Children.Count;
                            }

                            parent.Children.Insert(foundIdx, newFolder);


                            TreeViewItem tviParent = treeView.GetContainerFromItem(parent) as TreeViewItem;

                            if (tviParent != null)
                            {
                                tviParent.IsExpanded = true;
                            } 
                            
                            TreeViewItem tvi = treeView.GetContainerFromItem(newFolder) as TreeViewItem;

                            if (tvi != null)
                            {
                                tvi.IsSelected = true;
                            }
                        }
                    }
                }
            );
        }

        private void buttonFavouritesRename_Click(object sender, RoutedEventArgs e)
        {
            FavouriteBase item = treeView.SelectedItem as FavouriteBase;

            if (item != null)
            {
                Dialogs.FavouriteName dialog = new FavouriteName();

                dialog.ItemName = item.Name;

                DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "PROPERTIES", dialog, "textFavouriteName", DialogButtons.Ok | DialogButtons.Cancel, 
                    delegate(DialogResult dialogResult)
                    {
                        if (dialogResult == DialogResult.Ok)
                        {
                            item.Name = dialog.ItemName;
                        }
                    }
                );
            }
        }

        private void buttonFavouritesDelete_Click(object sender, RoutedEventArgs e)
        {
            FavouriteBase item = treeView.SelectedItem as FavouriteBase;
            if (item != null)
            {
                FavouriteFolder parent = FindParent(_rootFolder, item);

                if (parent != null)
                {
                    string res = item is FavouriteFolder ? "QUESTIONDELETEFAVOURITEFOLDER" : "QUESTIONDELETEFAVOURITE";

                    DialogPanel.ShowQuestionBox(CultureHelper.GetString(Properties.Resources.ResourceManager, res),
                        DialogButtons.Yes | DialogButtons.No, 
                        delegate(DialogResult dialogResult)
                        {
                            if (dialogResult == DialogResult.Yes)
                            {
                                parent.Children.Remove(item);
                            }
                        }
                    ); 
                }
            }
            UpdateButtonStates();
        }

        private void buttonFavouritesOpen_Click(object sender, RoutedEventArgs e)
        {
            Favourite favourite = treeView.SelectedItem as Favourite;
            if (favourite != null)
            {
                if (OpenFavourite != null)
                {
                    OpenFavourite(favourite);
                }
            }
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateButtonStates();
        }

        private void treeView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
#if SILVERLIGHT
            treeView.ScrollViewer.ScrollToVerticalOffset(treeView.ScrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
#endif
        }

        void treeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Favourite favourite = treeView.SelectedItem as Favourite;
            if (favourite != null)
            {
                if (OpenFavourite != null)
                {
                    OpenFavourite(favourite);
                }
            }
        }
        #endregion

        #region Private Methods
        private void UpdateButtonStates()
        {
            buttonFavouritesNewFolder.IsEnabled = true;
            buttonFavouritesRename.IsEnabled = treeView.SelectedItem != null;
            buttonFavouritesDelete.IsEnabled = treeView.SelectedItem != null;
            buttonFavouritesOpen.IsEnabled = treeView.SelectedItem is Favourite;
        }


        private FavouriteFolder FindParent(FavouriteFolder favouriteFolder, FavouriteBase favouriteItem)
        {
            FavouriteFolder foundParent = null;

            if (favouriteFolder.Children.Contains(favouriteItem))
            {
                foundParent = favouriteFolder;
            }
            else
            {
                for (int i = 0; i < favouriteFolder.Children.Count && foundParent == null; i++)
                {
                    FavouriteFolder childFolder = favouriteFolder.Children[i] as FavouriteFolder;

                    if (childFolder != null)
                    {
                        foundParent = FindParent(childFolder, favouriteItem);
                    }
                }
            }

            return (foundParent);
        }
        #endregion

        #region Drag And Drop
        
        void treeView_MoveToParent(object newParent, object item, bool addAtStart)
        {
            FavouriteBase favouriteBase = item as FavouriteBase;

            if (favouriteBase != null)
            {
                FavouriteFolder newFavouriteFolder;
                
                if(newParent == null)
                {
                    newFavouriteFolder = _rootFolder;
                }
                else
                {
                    newFavouriteFolder = newParent as FavouriteFolder;
                }

                if (newFavouriteFolder != null)
                {
                    FavouriteFolder originalFavouriteFolder = FindParent(_rootFolder, favouriteBase);

                    if (originalFavouriteFolder != null)
                    {
                        // Only move the item if the folder has really changed
                        if (originalFavouriteFolder != newFavouriteFolder)
                        {
                            originalFavouriteFolder.Children.Remove(favouriteBase);

                            // If we are adding a folder make sure it comes before any favourites
                            if (favouriteBase is FavouriteFolder)
                            {
                                int foundIdx = -1;

                                if (favouriteBase is FavouriteFolder && addAtStart)
                                {
                                    foundIdx = 0;
                                }
                                else
                                {
                                    for (int i = 0; i < newFavouriteFolder.Children.Count && foundIdx == -1; i++)
                                    {
                                        if (newFavouriteFolder.Children[i] is Favourite)
                                        {
                                            foundIdx = i;
                                        }
                                    }

                                    if (foundIdx == -1)
                                    {
                                        foundIdx = newFavouriteFolder.Children.Count;
                                    }
                                }

                                newFavouriteFolder.Children.Insert(foundIdx, favouriteBase);
                            }
                            else
                            {
                                newFavouriteFolder.Children.Add(favouriteBase);
                            }
                        }
                    }
                }
            }
        }

        void treeView_StartDrag(MouseEventArgs mouseEventsArgs, object item)
        {
            canvasDragDrop.Children.Clear();

            if (item != null)
            {
                ThemeBorder dragControl = new ThemeBorder();
                dragControl.Height = 30;
                dragControl.BorderThickness = new Thickness(1);

                TextBlock tb = new TextBlock();
                tb.VerticalAlignment = VerticalAlignment.Center; ;
                tb.Text = item.ToString();
                tb.Margin = new Thickness(5, 0, 5, 0);

                dragControl.Content = tb;

                Point mousePos = mouseEventsArgs.GetPosition(canvasDragDrop);

                Canvas.SetLeft(dragControl, mousePos.X - 10);
                Canvas.SetTop(dragControl, mousePos.Y - 10);

                dragControl.Opacity = 0.5;

                canvasDragDrop.Children.Clear();
                canvasDragDrop.Children.Add(dragControl);

                canvasDragDrop.CaptureMouse();

                _dragObject = item;
            }
        }

        private void canvasDragDrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void canvasDragDrop_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            canvasDragDrop.ReleaseMouseCapture();
            canvasDragDrop.Children.Clear();

            Point mousePosRoot = e.GetPosition(null);
            Point mousePosTreeView = e.GetPosition(treeView);

            treeView.DropObject(mousePosRoot, mousePosTreeView, _dragObject);
            _dragObject = null;
        }

        private void canvasDragDrop_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(canvasDragDrop);

            foreach (UIElement element in canvasDragDrop.Children)
            {
                Canvas.SetLeft(element, mousePos.X - 10);
                Canvas.SetTop(element, mousePos.Y - 10);
            }
        }
        #endregion


    }
}
