using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace LibXF.Controls
{
    public class BindableGrid : ContentView //  ItemsView<>?
    {
        // Basic
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create("ItemsSource", typeof(IEnumerable), typeof(BindableGrid));
        public IEnumerable ItemsSource { get => GetValue(ItemsSourceProperty) as IEnumerable; set => SetValue(ItemsSourceProperty, value); }

        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create("ItemTemplate", typeof(DataTemplate), typeof(BindableGrid));
        public DataTemplate ItemTemplate { get => GetValue(ItemTemplateProperty) as DataTemplate; set => SetValue(ItemTemplateProperty, value); }

        // Headers
        public static readonly BindableProperty RowHeadersProperty = BindableProperty.Create("RowHeaders", typeof(IEnumerable), typeof(BindableGrid));
        public IEnumerable RowHeaders { get => GetValue(RowHeadersProperty) as IEnumerable; set => SetValue(RowHeadersProperty, value); }

        public static readonly BindableProperty RowHeadersTemplateProperty = BindableProperty.Create("RowHeadersTemplate", typeof(DataTemplate), typeof(BindableGrid));
        public DataTemplate RowHeadersTemplate { get => (DataTemplate)GetValue(RowHeadersTemplateProperty); set => SetValue(RowHeadersTemplateProperty, value); }

        public static readonly BindableProperty ColumnHeadersProperty = BindableProperty.Create("ColumnHeaders", typeof(IEnumerable), typeof(BindableGrid));
        public IEnumerable ColumnHeaders { get => GetValue(ColumnHeadersProperty) as IEnumerable; set => SetValue(ColumnHeadersProperty, value); }

        public static readonly BindableProperty ColumnHeadersTemplateProperty = BindableProperty.Create("ColumnHeadersTemplate", typeof(DataTemplate), typeof(BindableGrid));
        public DataTemplate ColumnHeadersTemplate { get => (DataTemplate)GetValue(ColumnHeadersTemplateProperty); set => SetValue(ColumnHeadersTemplateProperty, value); }

        // Mashing it up
        public static readonly BindableProperty CellInfoProperty = BindableProperty.Create("CellInfo", typeof(CellInfoBinder), typeof(BindableGrid));
        public CellInfoBinder CellInfo { get => (CellInfoBinder)GetValue(CellInfoProperty); set => SetValue(CellInfoProperty, value); }

        // Frozen
        public static readonly BindableProperty FrozenHeadersProperty = BindableProperty.Create("FrozenHeaders", typeof(bool), typeof(BindableGrid), false);
        public bool FrozenHeaders { get => (bool)GetValue(FrozenHeadersProperty); set => SetValue(FrozenHeadersProperty, value); }

        // Loading
        public static readonly BindableProperty LoadingTemplateProperty = BindableProperty.Create("LoadingTemplate", typeof(DataTemplate), typeof(BindableGrid));
        public DataTemplate LoadingTemplate { get => (DataTemplate)GetValue(LoadingTemplateProperty); set => SetValue(LoadingTemplateProperty, value); }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            RecreateView();
        }
        readonly BindableProperty[] OurProps = new BindableProperty[] 
        {
            ItemsSourceProperty, ItemTemplateProperty,
            RowHeadersProperty, RowHeadersTemplateProperty,
            ColumnHeadersTemplateProperty, ColumnHeadersProperty,
            CellInfoProperty, FrozenHeadersProperty
        };
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if(OurProps.Any(x=>x.PropertyName == propertyName))
                RecreateView();
        }
        public event Action<Exception> RenderTaskFailure = delegate { };
        void RecreateView()
        {
            Content = new ActivityIndicator { IsRunning = true };
            var builder = new ContextGridBuilder(Device.BeginInvokeOnMainThread, x => RenderTaskFailure(x));
            builder.SetItems(ItemsSource);
            builder.SetItemTemplate(ItemTemplate);
            builder.SetHeaderTemplates(RowHeadersTemplate, ColumnHeadersTemplate);
            builder.AddHeaders(RowHeaders, ColumnHeaders);
            builder.UseCellInfoBinder(CellInfo);
            builder.FreezeHeaders(FrozenHeaders);
            var g = new Grid() { };
            if (LoadingTemplate != null)
                g.Children.Add(LoadingTemplate.CreateContent() as View);
            else g.Children.Add(new Label { Text = "Loading..." });
            builder.Build(g);
            Content = g;
        }
    }
    public class CellInfoBinder : BindableObject
    {
        public static readonly BindableProperty RowSpanProperty = BindableProperty.Create("RowSpan", typeof(int), typeof(CellInfoBinder), 1);
        public int RowSpan { get => (int)GetValue(RowSpanProperty); set => SetValue(RowSpanProperty, value); }

        public static readonly BindableProperty ColumnSpanProperty = BindableProperty.Create("ColumnSpan", typeof(int), typeof(CellInfoBinder),1);
        public int ColumnSpan { get => (int)GetValue(ColumnSpanProperty); set => SetValue(ColumnSpanProperty, value); }
    }
}