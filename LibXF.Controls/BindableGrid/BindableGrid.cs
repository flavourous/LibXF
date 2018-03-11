using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace LibXF.Controls.BindableGrid
{
    public interface ICellInfoManager
    {
        int GetRowSpan(Object cellData);
        int GetColumnSpan(Object cellData);
        double GetColumnmWidth(int col);
        double GetRowHeight(int row);
    }
    public class BindableGrid : ContentView //  ItemsView<>?
    {
        // Basic
        public static readonly BindableProperty CellsSourceProperty = BindableProperty.Create("CellsSource", typeof(IList<IList>), typeof(BindableGrid));
        public IList<IList> CellsSource { get => GetValue(CellsSourceProperty) as IList<IList>; set => SetValue(CellsSourceProperty, value); }

        public static readonly BindableProperty CellTemplateProperty = BindableProperty.Create("CellTemplate", typeof(DataTemplate), typeof(BindableGrid));
        public DataTemplate CellTemplate { get => GetValue(CellTemplateProperty) as DataTemplate; set => SetValue(CellTemplateProperty, value); }

        // Headers
        public static readonly BindableProperty RowHeadersSourceProperty = BindableProperty.Create("RowHeadersSource", typeof(IList<IList>), typeof(BindableGrid));
        public IList<IList> RowHeadersSource { get => GetValue(RowHeadersSourceProperty) as IList<IList>; set => SetValue(RowHeadersSourceProperty, value); }

        public static readonly BindableProperty ColumnHeadersSourceProperty = BindableProperty.Create("ColumnHeadersSource", typeof(IList<IList>), typeof(BindableGrid));
        public IList<IList> ColumnHeadersSource { get => GetValue(ColumnHeadersSourceProperty) as IList<IList>; set => SetValue(ColumnHeadersSourceProperty, value); }

        // Mashing it up
        public static readonly BindableProperty CellInfoProperty = BindableProperty.Create("CellInfo", typeof(ICellInfoManager), typeof(BindableGrid));
        public ICellInfoManager CellInfo { get => (ICellInfoManager)GetValue(CellInfoProperty); set => SetValue(CellInfoProperty, value); }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            RecreateView();
        }
        readonly BindableProperty[] OurProps = new BindableProperty[] 
        {
            CellsSourceProperty, CellTemplateProperty,
            RowHeadersSourceProperty, ColumnHeadersSourceProperty,
            CellInfoProperty
        };
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if(OurProps.Any(x=>x.PropertyName == propertyName))
                RecreateView();
        }
        void RecreateView()
        {
            Content = new VirtualizedGridLayout(CellsSource, CellTemplate, CellInfo);

        }
    }
    
}