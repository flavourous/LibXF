using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace LibXF.Controls
{
    public static class XFE
    {
        public static T Bind<T>(this T o, BindableProperty p, String path)
            where T : BindableObject
        {
            o.SetBinding(p, path);
            return o;
        }

        public static void ObserveProperty<T,O>(this BindableProperty bp, O on, Action<T,O> act)
            where  O :BindableObject
        {
            on.PropertyChanged += (o, e) =>
            {
                var obs = (O)o;
                if (e.PropertyName == bp.PropertyName)
                    act((T)obs.GetValue(bp), obs);
            };
        }
        public static void BindObservations<T,O>(this BindableProperty bp, Action<T,O> act, params O[] observables)
            where O : BindableObject
        {
            foreach (var obs in observables)
                ObserveProperty<T,O>(bp, obs, (nv, o) =>
                {
                    foreach (var ao in observables)
                        if (ao != o)
                            act(nv, ao);
                });
        }

    }
    [Flags]
    public enum GridBoundary { None=0, Top=1, Bottom=2, Left=4, Right=8, }
    public class CellContext : BindableObject
    {
        public GridBoundary Edge { get; set; }
        public Object Data { get; set; }
    }
    public class Gap
    {
        // Not implimented everywhere
        public Object Above { get; set; }
        public Object Below { get; set; }
        public Object Right { get; set; }
        public Object Left { get; set; }
    }
    internal class ContextGridBuilder
    {
        readonly Action<Action> DispatchAsync;
        public ContextGridBuilder(Action<Action> DispatchAsync)
        {
            this.DispatchAsync = DispatchAsync;
        }

        IEnumerable items;
        DataTemplate itemTemplate, cTemplate, rTemplate;

        public void SetItems(IEnumerable items) => this.items = items;

        public void SetItemTemplate(DataTemplate itemTemplate) => this.itemTemplate = itemTemplate;

        IEnumerable row, column;
        public void AddHeaders(IEnumerable row, IEnumerable column)
        {
            this.row = row;
            this.column = column;
        }

        public void SetHeaderTemplates(DataTemplate row, DataTemplate column)
        {
            rTemplate = row;
            cTemplate = column;
        }

        // row and column span
        CellInfoBinder infobinder;
        class CellInfo { public object item; public int rsp, csp; public bool isVoid; }
        public void UseCellInfoBinder(CellInfoBinder infobinder)
        {
            this.infobinder = infobinder;
        }

        bool frozen = false;
        public void FreezeHeaders(bool f) => frozen = f;

        public Grid Build()
        {
            Grid ret = new Grid();
            if (items == null || itemTemplate == null)
                return ret;

            Label ll = new Label { Text = "Loading..." };
            ret.Children.Add(ll);
            GridBuilder gb = new GridBuilder(DispatchAsync);

            Task.Run(() =>
            {
                if (!frozen)
                {
                    // build it as a stuck together grid
                    var stuck = gb.StickOnHeaders(items, row, column);
                    var mash = gb.MashItems(stuck.combo, infobinder);
                    gb.Build(ret, mash, (r, c) =>
                        {
                            if (c < stuck.cDepth && r >= stuck.rDepth && rTemplate != null)
                                return rTemplate;
                            if (r < stuck.rDepth && c >= stuck.cDepth && cTemplate != null)
                                return cTemplate;
                            return itemTemplate;
                        }, GridBoundary.None);
                }
                else
                {
                    // use 3 scrollviewers in a 2x2 grid, and bind the offsets and the row/col sizes
                    var mrow = gb.MashItems(row, infobinder);
                    var mcol = gb.MashItems(column, infobinder);
                    var mdat = gb.MashItems(items, infobinder);

                    Grid chGrid = null, rhGrid = null, fakeGrid = null, mGrid = null;

                    gb.Dispatch(() =>
                    {
                        ret.RowDefinitions.Add(GridBuilder.AutoRow);
                        ret.RowDefinitions.Add(new RowDefinition());
                        ret.ColumnDefinitions.Add(GridBuilder.AutoColumn);
                        ret.ColumnDefinitions.Add(new ColumnDefinition());

                        chGrid = new Grid { RowSpacing = 0, ColumnSpacing = 0 };
                        rhGrid = new Grid { RowSpacing = 0, ColumnSpacing = 0 };
                        fakeGrid = new Grid { RowSpacing = 0, ColumnSpacing = 0 };
                        mGrid = new Grid { RowSpacing = 0, ColumnSpacing = 0 };

                        // scrollers
                        var rh = new ScrollView { Content = rhGrid, Orientation = ScrollOrientation.Vertical };
                        var ch = new ScrollView { Content = chGrid, Orientation = ScrollOrientation.Horizontal };
                        var main = new ScrollView { Content = mGrid, Orientation = ScrollOrientation.Both, HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };

                        ret.Children.Add(fakeGrid);
                        ret.Children.Add(rh);
                        ret.Children.Add(ch);
                        ret.Children.Add(main);

                        // Scrollbar sync bindings
                        ScrollView.ScrollXProperty.BindObservations<double, ScrollView>((x, o) => o.ScrollToAsync(x, o.ScrollY, false).Wait(), main, ch);
                        ScrollView.ScrollYProperty.BindObservations<double, ScrollView>((y, o) => o.ScrollToAsync(o.ScrollX, y, false).Wait(), main, rh);

                        Grid.SetRow(rh, 1);
                        Grid.SetColumn(ch, 1);
                        Grid.SetRow(main, 1);
                        Grid.SetColumn(main, 1);

                    }).Wait();

                    // get fake gap bit contexts
                    var gapData = gb.BuildTopGap(mrow, mcol);

                    // grids
                    gb.Dispatch(() => ret.Children.Remove(ll)).Wait();
                    gb.Build(fakeGrid, gapData, (r, c) => itemTemplate, GridBoundary.Right | GridBoundary.Bottom);
                    gb.Build(rhGrid, mrow, (r, c) => rTemplate ?? itemTemplate, GridBoundary.Right);
                    gb.Build(chGrid, mcol, (r, c) => cTemplate ?? itemTemplate, GridBoundary.Bottom);
                    gb.Build(mGrid, mdat, (r, c) => itemTemplate, GridBoundary.Left | GridBoundary.Top, () =>
                    {
                        // grid sync bindings - chuck a boxview with size bound to size of foreign grid row/col
                        gb.EqualizeRowHeights(mGrid, rhGrid);
                        gb.EqualizeColumnWidths(mGrid, chGrid);
                        gb.EqualizeColumnWidths(fakeGrid, rhGrid);
                        gb.EqualizeRowHeights(fakeGrid, chGrid);
                    });

                }
            });
            return ret;
        }

        class GridBuilder
        {
            readonly Action<Action> DispatchAsync;
            public GridBuilder(Action<Action> DispatchAsync) => this.DispatchAsync = DispatchAsync;

            public async Task Dispatch(Action a)
            {
                TaskCompletionSource<int> tea = new TaskCompletionSource<int>();
                await Task.Delay(1);
                DispatchAsync(() =>
                {
                    try
                    {
                        a();
                    }
                    catch(Exception e)
                    {
                        tea.SetException(e);
                        return;
                    }
                    tea.SetResult(1);
                });
                await tea.Task;
            }

            class SizeSupport : View
            {
                public SizeSupport()
                {
                    HorizontalOptions = VerticalOptions = LayoutOptions.Start;
                    MinimumHeightRequest = MinimumWidthRequest = 1.0;
                    WidthRequest = HeightRequest = 1.0;
                    //BackgroundColor = Color.FromRgba(1, 0, 0, .2);
                }
            }
            class SizeMeasurer : SizeSupport
            {
                public SizeMeasurer()
                {
                    HorizontalOptions = VerticalOptions = LayoutOptions.Fill;
                 //   BackgroundColor = Color.FromRgba(0, 0, 1, .2);
                }
            }
            public static ColumnDefinition AutoColumn => new ColumnDefinition { Width = GridLength.Auto };
            public static RowDefinition AutoRow => new RowDefinition{ Height= GridLength.Auto };

            public void EqualizeRowHeights(Grid first, Grid second)
            {
                var com = Math.Min(first.RowDefinitions.Count, second.RowDefinitions.Count);
                for (int r = 0; r < com; r++)
                {
                    Dispatch(() =>
                    {
                        SizeSupport fs = new SizeSupport(), ss = new SizeSupport();
                        SizeMeasurer fm = new SizeMeasurer(), sm = new SizeMeasurer();
                        Grid.SetRow(fs, r);
                        Grid.SetRow(ss, r);
                        Grid.SetRow(fm, r);
                        Grid.SetRow(sm, r);
                        first.Children.Add(fs);
                        first.Children.Add(fm);
                        second.Children.Add(ss);
                        second.Children.Add(sm);
                        fs.SetBinding(View.HeightRequestProperty, new Binding(View.HeightProperty.PropertyName) { Source = sm });
                        ss.SetBinding(View.HeightRequestProperty, new Binding(View.HeightProperty.PropertyName) { Source = fm });
                    }).Wait();
                }
            }
            public void EqualizeColumnWidths(Grid first, Grid second)
            {
                var com = Math.Min(first.ColumnDefinitions.Count, second.ColumnDefinitions.Count);
                Dispatch(() =>
                {
                    for (int c = 0; c < com; c++)
                    {
                        SizeSupport fs = new SizeSupport(), ss = new SizeSupport();
                        SizeMeasurer fm = new SizeMeasurer(), sm = new SizeMeasurer();
                        Grid.SetColumn(fs, c);
                        Grid.SetColumn(ss, c);
                        Grid.SetColumn(fm, c);
                        Grid.SetColumn(sm, c);
                        first.Children.Add(fs);
                        first.Children.Add(fm);
                        second.Children.Add(ss);
                        second.Children.Add(sm);
                        fs.SetBinding(View.WidthRequestProperty, new Binding(View.WidthProperty.PropertyName) { Source = sm });
                        ss.SetBinding(View.WidthRequestProperty, new Binding(View.WidthProperty.PropertyName) { Source = fm });
                    }
                }).Wait();
            }

            public IEnumerable<IEnumerable<Object>> MashItems(IEnumerable items, CellInfoBinder infobinder)
            {
                if (infobinder == null) return items as IEnumerable<IEnumerable<object>>;

                // Expand the items, tagging with the cell info
                var eitems = items.Expand(x =>
                {
                    CellInfo ci = null;
                    // Bind into the info
                    Dispatch(() =>
                    {
                        infobinder.BindingContext = x;
                        ci = new CellInfo
                        {
                            item = x,
                            rsp = infobinder.RowSpan,
                            csp = infobinder.ColumnSpan
                        };
                    }).Wait();
                    return ci;
                });

                // Now we can re-iterate, and push along columns and rows with VoidCellContext to indicate they are empty
                for (int r = 0; r < eitems.Count; r++)
                {
                    var row = eitems[r];
                    for (int c = 0; c < row.Count; c++)
                    {
                        var cell = row[c];

                        // previously pushed
                        if (cell.isVoid)
                            continue;

                        // void the cells
                        for (int i = 0; i < cell.csp; i++)
                            for (int j = 0; j < cell.rsp; j++)
                                if (i > 0 || j > 0)
                                    eitems[r + j][c + i].isVoid = true;
                    }
                }

                return eitems;
            }

            public IEnumerable<IEnumerable<Object>> BuildTopGap(IEnumerable<IEnumerable<object>> mrow, IEnumerable<IEnumerable<object>> mcol)
            {
                var gapData = new List<List<Gap>>();
                int nch = mcol?.Count() ?? 0, nrh = (mrow?.Any() ?? false) ? mrow.Max(x => x?.Count() ?? 0) : 0;
                for (int r = 0; r < nch; r++)
                {
                    var gr = new List<Gap>();
                    for (int c = 0; c < nrh; c++)
                    {
                        var g = new Gap();
                        var lrGap = r == nch - 1;
                        var lcGap = c == nrh - 1;
                        if (lrGap) g.Below = mrow.FirstOrDefault()?.ElementAtOrDefault(c);
                        if (lcGap) g.Right = mcol.ElementAtOrDefault(r)?.FirstOrDefault();
                        gr.Add(g);
                    }
                    gapData.Add(gr);
                }
                return gapData;
            }

            public (IEnumerable combo, int rDepth, int cDepth) StickOnHeaders(IEnumerable items, IEnumerable row, IEnumerable column)
            {
                var cHeaders = column.Expand();
                var rHeaders = row.Expand();
                var data = items.Expand();

                var cHDepth = cHeaders.Count;
                var rHDepth = rHeaders.Count == 0 ? 0 : rHeaders.Max(x => x.Count);
                var drDepth = data.Count == 0 ? 0 : data.Max(x => x.Count);

                var tg = BuildTopGap(rHeaders, cHeaders);

                var topPart = tg.Zip(cHeaders, (g, h) => g.Concat(h));
                var mainpart = Enumerable.Range(0, Math.Max(data.Count, rHeaders.Count))
                                         .Select(x =>
                                         {
                                             var rhead = rHeaders.ElementAtOrDefault(x) ?? Enumerable.Repeat((object)new Gap(), rHDepth).ToList();
                                             var drow = data.ElementAtOrDefault(x) ?? Enumerable.Repeat((object)new Gap(), drDepth).ToList();
                                             return rhead.Concat(drow);
                                         });
                return (topPart.Concat(mainpart), rHDepth, cHDepth);
            }

            public Grid Build(Grid onto, IEnumerable items, Func<int,int,DataTemplate> tSelector, GridBoundary open, Action eq = null)
            {
                if (items == null) return onto;
                List<List<Object>> Contexts = new List<List<object>>();
                int rows = 0, cols = 0;
                foreach (var x in items)
                {
                    var il = new List<Object>();
                    Dispatch(() => onto.RowDefinitions.Add(GridBuilder.AutoRow)).Wait();
                    if (x is IEnumerable l)
                    {
                        int lcol = 0;
                        foreach (var y in l)
                        {
                            if (lcol >= cols)
                            {

                                Dispatch(() => onto.ColumnDefinitions.Add(GridBuilder.AutoColumn)).Wait();
                                cols++;
                            }
                            il.Add(y);
                            lcol++;
                        }
                    }
                    rows++;
                    Contexts.Add(il);
                }

                eq?.Invoke();

                // jagged, null for out of bounds
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        bool fr = r == 0, fc = c == 0, lr = r == rows - 1, lc = c == cols - 1;
                        var cell = Contexts[r].ElementAtOrDefault(c);
                        var inf = cell as CellInfo;
                        if (inf?.isVoid ?? false) continue;
                        var context = new CellContext
                        {
                            Data = inf?.item ?? cell,
                            Edge = ( (fr ? GridBoundary.Top : GridBoundary.None)
                                     | (fc ? GridBoundary.Left : GridBoundary.None)
                                     | (lr ? GridBoundary.Bottom : GridBoundary.None)
                                     | (lc ? GridBoundary.Right : GridBoundary.None)
                                   ) ^ open // remove any open boundaries
                        };
                        var dt = tSelector(r, c);
                        if (dt != null)
                        {
                            Dispatch(() =>
                            {
                                var cv = dt.CreateContent() as View;
                                cv.BindingContext = context;
                                Grid.SetRow(cv, r);
                                Grid.SetColumn(cv, c);
                                Grid.SetRowSpan(cv, inf?.rsp ?? 1);
                                Grid.SetColumnSpan(cv, inf?.csp ?? 1);
                                onto.Children.Add(cv);
                            }).Wait();
                        }
                    }
                }

                return onto;
            }
        }
    }
}
