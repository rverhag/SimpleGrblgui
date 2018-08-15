using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Vhr;
using Vhr.Gcode;

namespace VhR.SimpleGrblGui.Usercontrols
{
    
    public partial class GcodeControl : UserControl
    {
        private Grbl grbl;

        public GcodeControl()
        {
            InitializeComponent();

            DataContext = Grbl.Interface;

            grbl = Grbl.Interface;
            grbl.GcodeLineChanged += Grbl_GcodeLineChanged;
         
        }

        private void Grbl_GcodeLineChanged(object sender, EventArgs e)
        {
            SelectRowAsync((GcodeLine)sender);
        }

        //https://social.technet.microsoft.com/wiki/contents/articles/21202.wpf-programmatically-selecting-and-focusing-a-row-or-cell-in-a-datagrid.aspx
        public async void SelectRowAsync(GcodeLine _gcodeline)
        {
            await Task.Run(() => Dispatcher.BeginInvoke(new Action(delegate ()
            {
               // if (_gcodeline.InSerialBuffer)
                {
                   // GcodeGrid.SelectedItems.Clear();

                    object item = GcodeGrid.Items[_gcodeline.Index];
                    
                    GcodeGrid.SelectedItem = item;

                    if (grbl.InRunState)
                    {
                        GcodeGrid.ScrollIntoView(item);
                    }

                    //if (!(GcodeGrid.ItemContainerGenerator.ContainerFromIndex(_gcodeline.Index) is DataGridRow row))
                    //{
                    //    GcodeGrid.ScrollIntoView(item);
                    //    row = GcodeGrid.ItemContainerGenerator.ContainerFromIndex(_gcodeline.Index) as DataGridRow;
                    //}
                    ////row.Focus();
                    //DataGridCell cell = GetCell(row, 2);
                    //if (cell != null)
                    //{
                    //    cell.Focus();
                    //}
                }
            })));
        }

        public DataGridCell GetCell(DataGridRow rowContainer, int column)
        {
            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);
                if (presenter == null)
                {
                    /* if the row has been virtualized away, call its ApplyTemplate() method
                     * to build its visual tree in order for the DataGridCellsPresenter
                     * and the DataGridCells to be created */
                    rowContainer.ApplyTemplate();
                    presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);
                }
                if (presenter != null)
                {
                    DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
                    if (cell == null)
                    {
                        /* bring the column into view
                         * in case it has been virtualized away */
                        GcodeGrid.ScrollIntoView(rowContainer, GcodeGrid.Columns[column]);
                        cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
                    }
                    return cell;
                }
            }
            return null;
        }

        public IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            foreach (childItem child in FindVisualChildren<childItem>(obj))
            {
                return child;
            }

            return null;
        }
    }
}
