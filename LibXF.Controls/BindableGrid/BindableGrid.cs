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
        double GetColumnmWidth(int col, IList<IList> Source);
        double GetRowHeight(int row, IList<IList> Source);
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
            if (new object[] { CellInfo, CellsSource, CellTemplate }.All(x => x != null))
            {
                // header source defense
                var urh = RowHeadersSource ?? Enumerable.Empty<object>().Select(x => Enumerable.Empty<object>().ToList() as IList).ToList();
                var uch = ColumnHeadersSource ?? Enumerable.Empty<object>().Select(x => Enumerable.Empty<object>().ToList() as IList).ToList();

                // virtualizing grids
                var mainvg = new VirtualizedGridLayout(CellInfo, CellsSource, CellTemplate);
                var cheadvg = new VirtualizedGridLayout(CellInfo, uch, CellTemplate);
                var rheadvg = new VirtualizedGridLayout(CellInfo, urh, CellTemplate);

                // Generate and place vgl components
                var mainCells = new ScrollView { Content = mainvg, Orientation = ScrollOrientation.Both };
                var rHead = new ScrollView { Content = rheadvg, Orientation= ScrollOrientation.Vertical };
                var cHead = new ScrollView { Content = cheadvg, Orientation = ScrollOrientation.Horizontal };

                // figuring out heights - use the virtualized grid's info because it does proxies
                var cHeadHeight = uch.Count == 0 ? 0.0 : Enumerable.Range(0, uch.Count).Sum(x => cheadvg.info.GetRowHeight(x, uch));
                var maxcols = urh.Count == 0 ? 0 : urh.Max(x => x.Count);
                var rHeadWidth = maxcols == 0 ? 0.0 : Enumerable.Range(0, maxcols).Sum(x => rheadvg.info.GetColumnmWidth(x, urh));
                Grid.SetRow(rHead, 1);
                Grid.SetColumn(cHead, 1);
                Grid.SetRow(mainCells, 1);
                Grid.SetColumn(mainCells, 1);

                // Link the headers to the main
                mainCells.Scrolled += (o, e) =>
                {
                    rHead.ScrollToAsync(0, mainCells.ScrollY, false);
                }; mainCells.Scrolled += (o, e) =>
                {
                    cHead.ScrollToAsync(mainCells.ScrollX, 0, false);
                };
                cHead.Scrolled += (o, e) =>
                {
                    mainCells.ScrollToAsync(cHead.ScrollX, mainCells.ScrollY, false);
                };
                rHead.Scrolled += (o, e) =>
                {
                    mainCells.ScrollToAsync(mainCells.ScrollX, rHead.ScrollY, false);
                };

                Content = new Grid
                {
                    ColumnSpacing = 0,
                    RowSpacing = 0,
                    ColumnDefinitions =
                    {
                        new ColumnDefinition{ Width = rHeadWidth },
                        new ColumnDefinition{ Width = GridLength.Star },
                    },
                    RowDefinitions =
                    {
                        new RowDefinition{Height = cHeadHeight},
                        new RowDefinition { Height = GridLength.Star}
                    },
                    Children = { cHead, rHead, mainCells }
                };
            }
        }
    }
    
}