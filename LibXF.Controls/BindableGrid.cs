using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace LibXF.Controls.BindableLayout
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
        public static readonly BindableProperty LoadingTemplateProperty = BindableProperty.Create("LoadingTemplate", typeof(View), typeof(BindableGrid));
        public View LoadingTemplate { get => (View)GetValue(LoadingTemplateProperty); set => SetValue(LoadingTemplateProperty, value); }

        // Dispatcher!
        public static readonly BindableProperty UIDispatcherProperty = BindableProperty.Create("UIDispatcher", typeof(ITimedDispatcher), typeof(BindableGrid), new TimedDispatcher(Device.BeginInvokeOnMainThread));
        public ITimedDispatcher UIDispatcher { get => (ITimedDispatcher)GetValue(UIDispatcherProperty); set => SetValue(UIDispatcherProperty, value); }


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
            var builder = new ContextGridBuilder(UIDispatcher, x => RenderTaskFailure(x));
            builder.SetItems(ItemsSource);
            builder.SetItemTemplate(ItemTemplate);
            builder.SetHeaderTemplates(RowHeadersTemplate, ColumnHeadersTemplate);
            builder.AddHeaders(RowHeaders, ColumnHeaders);
            builder.UseCellInfoBinder(CellInfo);
            builder.FreezeHeaders(FrozenHeaders);
            var g = new Grid();
            var lt = LoadingTemplate ?? new Grid
            {
                Children =
                {
                    new Frame
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Content= new StackLayout
                        {
                            Margin = new Thickness(10,5),
                            Spacing = 0,
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.Center,
                            Orientation = StackOrientation.Vertical,
                            Children =
                            {
                                new Label
                                {
                                    HorizontalOptions =LayoutOptions.Center,
                                    Text = "Loading",
                                    TextColor = Color.White,
                                }
                            }
                        },
                        BackgroundColor = Color.FromHex("88000000")
                    }
                },
                BackgroundColor = Color.FromHex("88FFFFFF")
            };
            var lg = new Grid { Children = { g, lt } };
            Content = lg;
            builder.Build(g).ContinueWith(t => Device.BeginInvokeOnMainThread(() => lg.Children.Remove(lt)));
        }
    }
    public class CellInfoBinder : BindableObject
    {
        public static readonly BindableProperty RowSpanProperty = BindableProperty.Create("RowSpan", typeof(int), typeof(CellInfoBinder), 1);
        public int RowSpan { get => (int)GetValue(RowSpanProperty); set => SetValue(RowSpanProperty, value); }

        public static readonly BindableProperty ColumnSpanProperty = BindableProperty.Create("ColumnSpan", typeof(int), typeof(CellInfoBinder),1);
        public int ColumnSpan { get => (int)GetValue(ColumnSpanProperty); set => SetValue(ColumnSpanProperty, value); }

        public static readonly BindableProperty WidthProperty = BindableProperty.Create("Width", typeof(double), typeof(CellInfoBinder), 50.0);
        public double Width { get => (double)GetValue(WidthProperty); set => SetValue(WidthProperty, value); }

        public static readonly BindableProperty HeightProperty = BindableProperty.Create("Height", typeof(double), typeof(CellInfoBinder), 50.0);
        public double Height { get => (double)GetValue(HeightProperty); set => SetValue(HeightProperty, value); }
    }
}