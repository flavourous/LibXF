using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xamarin.Forms;
using System.Threading;
using System.Threading.Tasks;

namespace LibXF.Controls.BindableGrid
{
    public class VirtualizedGridLayout : Layout<View>
    {
        public readonly ICellInfoManager info;
        readonly IList<IList> cells;
        readonly DataTemplate template;
        readonly VTools rScanner, cScanner;

        public VirtualizedGridLayout(ICellInfoManager info, IList<IList> cells, DataTemplate template)
        {
            var vv = new InfoMeasurerProxy(MeasureRow, MeasureColumn);
            vv.SetInfo(info);
            this.info = vv;
            this.cells = cells;
            this.template = template;
            rScanner = new VTools(x=>info.GetRowHeight(x,cells));
            cScanner = new VTools(x=>info.GetColumnmWidth(x,cells));
            Children.Add(new BoxView { WidthRequest = 0, HeightRequest = 0, BackgroundColor = Color.Transparent }); // forces onlayout to be called
        }

        class NullCol { }
        double MeasureColumn(int col)
        {
            using (var tm = new TempMeasureHelper(this))
            {
                var els = cells.Select(x => col < x.Count ? x[col] : new NullCol());
                if (els.All(x => x is NullCol)) return 0;
                var ret = els.Select((x, i) =>
                {
                    var ck = (i, col);
                    if (CellViewIndex.ContainsKey(ck))
                        return CellViewIndex[ck].Width;
                    else return tm.Measure(x).Request.Width;
                }).Max();
                return ret;
            }
        }
        double MeasureRow(int row)
        {
            using (var tm = new TempMeasureHelper(this))
            {
                if (row >= cells.Count()) return 0;
                var ret = cells.ElementAt(row).Cast<Object>().Select((x, i) =>
                {
                    var ck = (row, i);
                    if (CellViewIndex.ContainsKey(ck))
                        return CellViewIndex[ck].Height;
                    else return tm.Measure(x).Request.Height;
                }).Max();
                return ret;
            }
        }

        class TempMeasureHelper :IDisposable
        {
            readonly VirtualizedGridLayout vgl;
            public TempMeasureHelper(VirtualizedGridLayout vgl)
            {
                this.vgl = vgl;
            }
            View v = null;
            Object lasto;
            public SizeRequest Measure(object o)
            {
                if(v == null || o?.GetType() != lasto?.GetType())
                {
                    lasto = o;
                    if(v != null) vgl.Children.Remove(v);
                    v = vgl.CreateFromTemplate(o);
                    vgl.Children.Add(v);
                }
                v.BindingContext = o;
                return v.Measure(double.PositiveInfinity, double.PositiveInfinity, MeasureFlags.IncludeMargins);
            }
            public void Dispose()
            {
                if (v != null) vgl.Children.Remove(v);
            }
        }

        HashSet<String> ObservedProperties = new HashSet<string>
        {
            ScrollView.ScrollXProperty.PropertyName,
            ScrollView.ScrollYProperty.PropertyName,
            View.WidthProperty.PropertyName,
            View.HeightProperty.PropertyName
        };
        void peh(object sender, PropertyChangedEventArgs args)
        {
            if(ObservedProperties.Contains(args.PropertyName))
            {
                InvalidateViewportCells();
            }
        }
        INotifyPropertyChanged last;
        protected override void OnParentSet()
        {
            base.OnParentSet();
            if (last != null) last.PropertyChanged -= peh;
            if (Parent != null)
            {
                last = Parent;
                last.PropertyChanged += peh;
                InvalidateExtent();
                InvalidateViewportCells();
            }
        }
        // recalculate them if required, intent to give LayoutChildren enough information to reogranize and recycle
        double lSX, lSY, lW, lH;
        private void InvalidateViewportCells()
        {
            double sx = 0.0, sy = 0.0, w = 0.0, h = 0.0;
            if (Parent is ScrollView lps)
            {
                sx = lps.ScrollX;
                sy = lps.ScrollY;
                w = lps.Width;
                h = lps.Height;
            }
            else if (Parent is View v)
            {
                w = Width;
                h = Height;
            }
            if (lSX != sx || lSY != sy || lW != w || lH != h || runLayout)
            {
                // cache buster
                runLayout = true;
                runScan = true;
                lSX = sx;
                lSY = sy;
                lW = w;
                lH = h;
                ProcessChildren();
            }
        }

        protected override bool ShouldInvalidateOnChildRemoved(View child) => false;
        protected override bool ShouldInvalidateOnChildAdded(View child) => false;

#warning not handling row/col span where the span into view from outside calculated buffer bounds
#warning not caching GetRowHeight or GetColumnWidth calls appropriately

        readonly Dictionary<(int r, int c), View> CellViewIndex = new Dictionary<(int r, int c), View>();

        class DoScanResult
        {
            public ScanResult rowRange, colRange;
            public Queue<KeyValuePair<(int r, int c), View>> recyclable;
        }
        DoScanResult DoScan()
        {
            // setup scanners
            var nrows = cells.Count;
            var ncols = cells.Count == 0 ? 0 : cells.Max(c => c.Count);

            // Get bounds
            CalculateExtent();
            var rowRange = rScanner.Update(lSY, lH, eheight, nrows);
            var colRange = cScanner.Update(lSX, lW, ewidth, ncols);

            // Build a queue of recyclable cells
            var recyclableQuery = CellViewIndex.Where(kv => kv.Key.c < colRange.first ||
                                                            kv.Key.c > colRange.last ||
                                                            kv.Key.r < rowRange.first ||
                                                            kv.Key.r > rowRange.last);
            var recyclable = new Queue<KeyValuePair<(int r, int c), View>>(recyclableQuery);

            return new DoScanResult { rowRange = rowRange, colRange = colRange, recyclable = recyclable };
        }

        DoScanResult scan = null;
        bool runScan = false;
        bool runLayout = false;
        void ProcessChildren()
        {
            // incremental layout
            var sw = Stopwatch.StartNew();
            int maxMS = 125;

            if (runScan || scan == null)
                scan = DoScan();
            runScan = false;

            // Update index and layout views
            double ly = scan.rowRange.placement;
            for (int r = scan.rowRange.first; r <= scan.rowRange.last; r++, ly += info.GetRowHeight(r, cells))
            {
                double lx = scan.colRange.placement;
                for (int c = scan.colRange.first; c <= scan.colRange.last; c++, lx += info.GetColumnmWidth(c, cells))
                {
                    // Interleave progressively
                    if (sw.ElapsedMilliseconds > maxMS)
                    {
                        var myScan = scan;
                        Task.Run(async () =>
                        {
                            await Task.Delay(25);
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                if (scan != myScan) return; // re-scanned! stop!
                                ProcessChildren(); // context switch splits the stack
                            });
                        });
                        return;
                    }

                    // Setup and check
                    var rc = (r, c);
                    var cc = cells[r][c];
                    if (cc == null) continue;

                    // Size should be
                    double cheight = Enumerable.Range(r, info.GetRowSpan(cc)).Sum(z => info.GetRowHeight(z, cells));
                    double cwidth = Enumerable.Range(c, info.GetColumnSpan(cc)).Sum(z => info.GetColumnmWidth(z, cells));
                    var lrect = new Rectangle(lx, ly, cwidth, cheight);

                    // get child to layout
                    View container = null;
                    if (CellViewIndex.ContainsKey(rc))
                    {
                        container = CellViewIndex[rc];
                        if (container.Bounds == lrect) continue;
                    }
                    else
                    {
                        // Generate container
                        if (scan.recyclable.Count > 0)
                        {
                            var dq = scan.recyclable.Dequeue();
                            CellViewIndex.Remove(dq.Key);
                            container = dq.Value;
                        }
                        else
                        {
                            container = CreateFromTemplate(cc);
                            Children.Add(container);
                        }
                        container.BindingContext = cc;

                        // Update index
                        CellViewIndex[rc] = container;
                    }

                    // Layout cell - ignore layoutoptions
                    container.Layout(lrect);
                }
            }

            // Delete any unrecycled
            while (scan.recyclable.Count > 0)
            {
                var rcy = scan.recyclable.Dequeue();
                Children.Remove(rcy.Value);
                CellViewIndex.Remove(rcy.Key);
            }
            scan = null; // Scan is completed
        }

        View CreateFromTemplate(object context)
        {
            var uTemplate = template;
            if (template is DataTemplateSelector dts)
                uTemplate = dts.SelectTemplate(context, this);

            return uTemplate.CreateContent() as View;
        }

        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            // dont care.
        }


        // Extent info
        int nrow, ncol;
        double eheight, ewidth;

        bool update = true;
        void InvalidateExtent()
        {
            update = true;
            InvalidateMeasure();
        }
        void CalculateExtent()
        {
            if(update)
            {
                nrow = cells.Count;
                ncol = cells.Count == 0 ? 0 : cells.Max(x => x.Count);
                eheight = Enumerable.Range(0, nrow).Sum(x => info.GetRowHeight(x, cells));
                ewidth = Enumerable.Range(0, ncol).Sum(x => info.GetColumnmWidth(x, cells));
                update = false;
                InvalidateViewportCells();
            }
        }

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            CalculateExtent();
            return new SizeRequest(new Size(ewidth, eheight));
        }
    }
}
