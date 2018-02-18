using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace LibXF.Controls.BindableLayout
{
    public static class XFE
    {
        public static RowDefinition Row(this GridLength l) => new RowDefinition { Height = l };
        public static ColumnDefinition Column(this GridLength l) => new ColumnDefinition { Width = l };
        public static DataTemplate SelectDataTemplate(this DataTemplate self, object item, BindableObject container)
        {
            var selector = self as DataTemplateSelector;
            if (selector == null)
                return self;

            return selector.SelectTemplate(item, container);
        }

        public static void EqualizeMax<T,V, O>(T a, T b, Func<T,IEnumerable<V>> enumerate, Func<V,O> get, Action<V,O> set, Func<O,O,bool> bigger)
        {
            foreach(var pair in enumerate(a).Zip(enumerate(b), (first, second) => new { first, second }))
            {
                var f = get(pair.first);
                var s = get(pair.second);
                if (bigger(f, s)) set(pair.second, f);
                else if (bigger(s, f)) set(pair.first, s);
            }
        }

        public static async Task EqualizeMaxAsync<T, V, O>(T a, T b, Func<T, Task<IEnumerable<V>>> enumerate, Func<V, Task<O>> get, Func<V, O, Task> set, Func<O, O, bool> bigger)
        {
            List<Task> alls = new List<Task>();
            foreach (var pair in enumerate(a).Result.Zip(enumerate(b).Result, (first, second) => new { first, second }))
            {
                var pairVal = Task.WhenAll(get(pair.first), get(pair.second));
                var eqalised = pairVal.ContinueWith(async tks =>
                {
                    var f = tks.Result[0];
                    var s = tks.Result[1];
                    if (bigger(f, s)) await set(pair.second, f);
                    else if (bigger(s, f)) await set(pair.first, s);
                });
                alls.Add(eqalised);
            }
            await Task.WhenAll(alls.ToArray());
        }

        public static object CreateContent(this DataTemplate self, object item, BindableObject container)
        {
            return self.SelectDataTemplate(item, container).CreateContent();
        }

        public static T Bind<T>(this T o, BindableProperty p, String path)
            where T : BindableObject
        {
            o.SetBinding(p, path);
            return o;
        }

        public static void ObserveProperty<T, O>(this BindableProperty bp, O on, Action<T, O> act)
            where O : BindableObject
        {
            on.PropertyChanged += (o, e) =>
            {
                var obs = (O)o;
                if (e.PropertyName == bp.PropertyName)
                    act((T)obs.GetValue(bp), obs);
            };
        }
        public static void BindObservations<T, O>(this BindableProperty bp, Action<T, O> act, params O[] observables)
            where O : BindableObject
        {
            foreach (var obs in observables)
                ObserveProperty<T, O>(bp, obs, (nv, o) =>
                 {
                     foreach (var ao in observables)
                         if (ao != o)
                             act(nv, ao);
                 });
        }

    }
    [Flags]
    public enum GridBoundary { None = 0, Top = 1, Bottom = 2, Left = 4, Right = 8, }
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
        readonly ITimedDispatcher dispatcher;
        readonly Action<Exception> RenderFailure;
        public ContextGridBuilder(ITimedDispatcher dispatcher, Action<Exception> RenderFailure)
        {
            this.dispatcher = dispatcher;
            this.RenderFailure = RenderFailure;
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
        class CellInfo { public object item; public int rsp, csp; public bool isVoid; public double cw, ch; }
        public void UseCellInfoBinder(CellInfoBinder infobinder)
        {
            this.infobinder = infobinder;
        }

        bool frozen = false;
        public void FreezeHeaders(bool f) => frozen = f;

        public async Task Build(Grid g)
        {
            if (items == null || itemTemplate == null)
                return;

            GridBuilder gb = new GridBuilder(dispatcher);

            await Task.Run(async () =>
            {
                if (!frozen)
                {
                    // build it as a stuck together grid
                    var stuck = gb.StickOnHeaders(items, row, column);
                    var mash = await gb.WrapWithCellInfo(stuck.combo, infobinder);
                    await gb.Build(g, mash, (r, c) =>
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
                    var tmrow = gb.WrapWithCellInfo(row, infobinder);
                    var tmcol = gb.WrapWithCellInfo(column, infobinder);
                    var tmdat = gb.WrapWithCellInfo(items, infobinder);
                    var mrow = await tmrow;
                    var mcol = await tmcol;
                    var mdat = await tmdat;

                    Grid chGrid = null, rhGrid = null, fakeGrid = null, mGrid = null;

                    await gb.Dispatch(() =>
                    {
                        g.RowDefinitions.Add(GridLength.Auto.Row());
                        g.RowDefinitions.Add(GridLength.Star.Row());
                        g.ColumnDefinitions.Add(GridLength.Auto.Column());
                        g.ColumnDefinitions.Add(GridLength.Star.Column());

                        chGrid = new Grid { RowSpacing = 0, ColumnSpacing = 0 };
                        rhGrid = new Grid { RowSpacing = 0, ColumnSpacing = 0 };
                        fakeGrid = new Grid { RowSpacing = 0, ColumnSpacing = 0 };
                        mGrid = new Grid { RowSpacing = 0, ColumnSpacing = 0 };

                        // scrollers
                        var rh = new ScrollView { Content = rhGrid, Orientation = ScrollOrientation.Vertical };
                        var ch = new ScrollView { Content = chGrid, Orientation = ScrollOrientation.Horizontal };
                        var main = new ScrollView { Content = mGrid, Orientation = ScrollOrientation.Both, HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };

                        g.Children.Add(fakeGrid);
                        g.Children.Add(rh);
                        g.Children.Add(ch);
                        g.Children.Add(main);

                        // Scrollbar sync bindings
                        ScrollView.ScrollXProperty.BindObservations<double, ScrollView>((x, o) => o.ScrollToAsync(x, o.ScrollY, false), main, ch);
                        ScrollView.ScrollYProperty.BindObservations<double, ScrollView>((y, o) => o.ScrollToAsync(o.ScrollX, y, false), main, rh);

                        Grid.SetRow(rh, 1);
                        Grid.SetColumn(ch, 1);
                        Grid.SetRow(main, 1);
                        Grid.SetColumn(main, 1);

                    });

                    // get fake gap bit contexts
                    var gapData = await gb.WrapWithCellInfo(gb.BuildTopGap(mrow, mcol), infobinder);

                    // grids
                    await Task.WhenAll
                    (
                        gb.Build(fakeGrid, gapData, (r, c) => itemTemplate, GridBoundary.Right | GridBoundary.Bottom),
                        gb.Build(rhGrid, mrow, (r, c) => rTemplate ?? itemTemplate, GridBoundary.Right),
                        gb.Build(chGrid, mcol, (r, c) => cTemplate ?? itemTemplate, GridBoundary.Bottom),
                        gb.Build(mGrid, mdat, (r, c) => itemTemplate, GridBoundary.Left | GridBoundary.Top)
                    );

                    //equalize rows
                    await Task.WhenAll
                    (
                        gb.EqualizeRowsMax(fakeGrid, chGrid),
                        gb.EqualizeColsMax(fakeGrid, rhGrid),
                        gb.EqualizeColsMax(mGrid, chGrid),
                        gb.EqualizeRowsMax(mGrid, rhGrid)
                    );
                }
            }).ContinueWith(x => { if (x.IsFaulted) RenderFailure(x.Exception); });
        }

        class GridBuilder
        {
            readonly ITimedDispatcher dispatcher;
            public GridBuilder(ITimedDispatcher dispatcher) => this.dispatcher = dispatcher;
            
            public async Task Dispatch(Action a)
            {
                var completed = await dispatcher.Add(a);
                await completed;
            }
            public async Task<T> Dispatch<T>(Func<T> a)
            {
                var result = await dispatcher.Add(a);
                return await result;
            }

            bool GGreater(GridLength a, GridLength b)
            {
                if (a.IsAbsolute && b.IsAbsolute)
                {
                    return a.Value > b.Value;
                }
                return false;
            }

            public Task EqualizeRowsMax(Grid g, Grid o)
            {
                return XFE.EqualizeMaxAsync
                (
                    g,
                    o,
                    x => Dispatch(() => x.RowDefinitions as IEnumerable<RowDefinition>),
                    y => Dispatch(() => y.Height),
                    (x, y) => Dispatch(() => x.Height = y),
                    GGreater
                );
            }
            public Task EqualizeColsMax(Grid g, Grid o)
            {
                return XFE.EqualizeMaxAsync
                (
                    g,
                    o,
                    x => Dispatch(() => x.ColumnDefinitions as IEnumerable<ColumnDefinition>),
                    y => Dispatch(() => y.Width),
                    (x, y) => Dispatch(() => x.Width = y),
                    GGreater
                );
            }

            async Task DispatchCellInfo(List<List<CellInfo>> eitems, CellInfoBinder infobinder)
            {
                var ib = infobinder ?? new CellInfoBinder();

                // Dispatch info binder calls
                List<Task> calls = new List<Task>();
                foreach (var r in eitems)
                {
                    foreach (var c in r)
                    {
                        var ci = c;
                        var task = Dispatch(() =>
                        {
                            // Bind into the info
                            ib.BindingContext = ci.item;
                            ci.rsp = ib.RowSpan;
                            ci.csp = ib.ColumnSpan;
                            ci.cw = ib.Width;
                            ci.ch = ib.Height;
                        });
                        calls.Add(task);
                    }
                }
                await Task.WhenAll(calls);
            }

            public async Task<IEnumerable<IEnumerable<CellInfo>>> WrapWithCellInfo(IEnumerable items, CellInfoBinder infobinder)
            {
                // Expand the items, preparing cellinfo containers
                var eitems = items.Expand(x => new CellInfo { item = x });

                // continue when all that dispatcher work is done
                await DispatchCellInfo(eitems, infobinder);

                // Now we can re-iterate, and push along columns and rows with VoidCellContext to indicate they are empty
                for (int r = 0; r < eitems.Count; r++)
                {
                    var row = eitems[r];
                    for (int c = 0; c < row.Count; c++)
                    {
                        VoidCells(eitems, r, c);
                    }
                }

                return eitems;
            }

            void VoidCells(List<List<CellInfo>> eitems, int r, int c)
            {
                var cell = eitems[r][c];

                // previously pushed
                if (cell.isVoid)
                    return;

                // void the cells
                for (int i = 0; i < cell.csp; i++)
                    for (int j = 0; j < cell.rsp; j++)
                        if (i > 0 || j > 0)
                        {
                            if (r + j >= eitems.Count || c + i >= eitems[r + j].Count)
                                throw new IndexOutOfRangeException(
                                    String.Format("Attempting to void a cell that is off-grid, is your span too big?{7}" +
                                                  "Rows:{0}{7}Row:{1}{7}Cells:{2}{7}Cell:{3}{7}ColumnSpan:{4}{7}RowSpan:{5}{7}Context:{6}",
                                                  eitems.Count, r, eitems[r].Count, c, cell.csp, cell.rsp, cell.item.ToString(), Environment.NewLine)
                                    );
                            eitems[r + j][c + i].isVoid = true;
                        }
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
                        if (g.Below is CellInfo bci) g.Below = bci.item;
                        if (g.Right is CellInfo bcr) g.Right = bcr.item;
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

            public async Task Build(Grid onto, IEnumerable items, Func<int, int, DataTemplate> tSelector, GridBoundary open)
            {
                if (items == null) return ;
                Dictionary<int, double> rowHeights = new Dictionary<int, double>(), colWidths = new Dictionary<int, double>();
                List<List<CellInfo>> Contexts = items.Expand((x,r,c)=>
                {
                    var ci = x as CellInfo;
                    rowHeights[r] = rowHeights.ContainsKey(r) ? Math.Max(rowHeights[r], ci.ch) : Math.Max(0, ci.ch);
                    colWidths[c] = colWidths.ContainsKey(c) ? Math.Max(colWidths[c], ci.cw) : Math.Max(0, ci.cw);
                    return ci;
                });
                int rows = rowHeights.Count, cols = colWidths.Count;
                await Dispatch(() =>
                {
                    foreach (var kv in rowHeights)
                        onto.RowDefinitions.Add(new GridLength(kv.Value).Row());
                    foreach (var kv in colWidths)
                        onto.ColumnDefinitions.Add(new GridLength(kv.Value).Column());
                });

                List<Task> toAwait = new List<Task>();

                // jagged, null for out of bounds
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        bool fr = r == 0, fc = c == 0, lr = r == rows - 1, lc = c == cols - 1;
                        var cell = Contexts[r].ElementAtOrDefault(c);
                        var inf = cell as CellInfo;
                        if (inf.isVoid) continue;
                        var context = new CellContext
                        {
                            Data = inf.item,
                            Edge = ((fr ? GridBoundary.Top : GridBoundary.None)
                                     | (fc ? GridBoundary.Left : GridBoundary.None)
                                     | (lr ? GridBoundary.Bottom : GridBoundary.None)
                                     | (lc ? GridBoundary.Right : GridBoundary.None)
                                   ) ^ open // remove any open boundaries
                        };
                        var dt = tSelector(r, c);
                        if (dt != null)
                        {
                            int capture_c = c, capture_r = r;
                            var dtk = Dispatch(() =>
                              {
                                  var cv = dt.CreateContent(context.Data, onto) as View;
                                  cv.BindingContext = context;
                                  Grid.SetRow(cv, capture_r);
                                  Grid.SetColumn(cv, capture_c);
                                  Grid.SetRowSpan(cv, inf.rsp);
                                  Grid.SetColumnSpan(cv, inf.csp);

                                  onto.Children.Add(cv);
                                  if (inf.cw == -1.0 || inf.ch == -1.0)
                                  {
                                    // measurment time
                                    var desired = cv.Measure
                                    (
                                        inf.cw == -1 ? double.PositiveInfinity : inf.cw,
                                        inf.ch == -1 ? double.PositiveInfinity : inf.ch,
                                        MeasureFlags.IncludeMargins
                                    );
                                      var dw = desired.Request.Width;
                                      var dh = desired.Request.Height;
                                      if (dw != double.PositiveInfinity && dw > colWidths[capture_c])
                                          onto.ColumnDefinitions[capture_c].Width = colWidths[capture_c] = dw;
                                      if (dh != double.PositiveInfinity && dh > rowHeights[capture_r])
                                          onto.RowDefinitions[capture_r].Height = rowHeights[capture_r] = dh;
                                  }
                              });
                            toAwait.Add(dtk);
                        }
                    }
                }

                await Task.WhenAll(toAwait);
            }
        }
    }
}
