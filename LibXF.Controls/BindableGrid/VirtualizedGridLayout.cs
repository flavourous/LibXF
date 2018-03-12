using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Xamarin.Forms;
using System.Threading;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reactive;

namespace LibXF.Controls.BindableGrid
{
    public class VirtualizedGridLayout : Layout<View>
    {
        readonly ICellInfoManager info;
        readonly IList<IList> cells;
        readonly DataTemplate template;
        readonly VTools rScanner, cScanner;

        event EventHandler<EventArgs> DeferPartialLayout = delegate { };
        readonly MeasureType mt;
        public VirtualizedGridLayout(ICellInfoManager info, IList<IList> cells, DataTemplate template, MeasureType mt)
        {
            this.mt = mt;
            this.info = info;
            this.cells = cells;
            this.template = template;
            rScanner = new VTools(x=>info.GetRowHeight(x,mt));
            cScanner = new VTools(x=>info.GetColumnmWidth(x,mt));
            Children.Add(new BoxView { WidthRequest = 0, HeightRequest = 0, BackgroundColor = Color.Transparent }); // forces onlayout to be called

            Observable.FromEventPattern<EventArgs>(h => DeferPartialLayout += h, h => DeferPartialLayout -= h)
                      .Buffer(TimeSpan.FromMilliseconds(50)) // lets treat it like a delay
                      .Where(x => x.Count() > 0)
                      .ObserveOn(SynchronizationContext.Current)
                      .Subscribe(args => OurLayout());
        }

        HashSet<String> ObservedProperties = new HashSet<string>
        {
            ScrollView.ScrollXProperty.PropertyName,
            ScrollView.ScrollYProperty.PropertyName,
            View.WidthProperty.PropertyName,
            View.HeightProperty.PropertyName
        };
        IDisposable lastSubscriber;
        protected override void OnParentSet()
        {
            lock (mtl)
            {
                base.OnParentSet();
                if (lastSubscriber != null)
                    lastSubscriber.Dispose();
                if (Parent != null)
                {
                    lastSubscriber = Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(h => Parent.PropertyChanged += h, h => Parent.PropertyChanged -= h)
                                               .Buffer(TimeSpan.FromMilliseconds(20))
                                               .Select(x => x.Select(y => y.EventArgs.PropertyName).Where(ObservedProperties.Contains))
                                               .Select(x => x.Distinct())
                                               .Where(x => x.Count() > 0)
                                               .ObserveOn(SynchronizationContext.Current)
                                               .Subscribe(x => InvalidateViewportCells());
                    InvalidateExtent();
                    InvalidateViewportCells();
                }
            }
        }
        // recalculate them if required, intent to give LayoutChildren enough information to reogranize and recycle
        object mtl = new object();
        double lSX, lSY, lW, lH;
        private void InvalidateViewportCells()
        {
            lock (mtl)
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
                    OurLayout();
                }
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
        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            // XF you suck.
        }
        void OurLayout()
        { 
            lock (mtl)
            {
                // incremental layout
                var sw = Stopwatch.StartNew();
                int maxMS = 50;

                // blocks recursion and layouts we may not need to perform
                if (!runLayout) return;
                runLayout = false;

                if (runScan || scan == null)
                    scan = DoScan();
                runScan = false;

                // Update index and layout views
                double ly = scan.rowRange.placement;
                for (int r = scan.rowRange.first; r <= scan.rowRange.last; r++, ly += info.GetRowHeight(r, mt))
                {
                    double lx = scan.colRange.placement;
                    for (int c = scan.colRange.first; c <= scan.colRange.last; c++, lx += info.GetColumnmWidth(c, mt))
                    {
                        // Interleave progressively
                        if (sw.ElapsedMilliseconds > maxMS)
                        {
                            runLayout = true;
                            DeferPartialLayout(this, new EventArgs());
                            return;
                        }

                        // Setup and check
                        var rc = (r, c);
                        var cc = cells[r][c];
                        if (cc == null) continue;

                        // Size should be
                        double cheight = Enumerable.Range(r, info.GetRowSpan(cc)).Sum(z => info.GetRowHeight(z, mt));
                        double cwidth = Enumerable.Range(c, info.GetColumnSpan(cc)).Sum(z => info.GetColumnmWidth(z, mt));
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
                                container = template.CreateContent() as View;
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
                eheight = Enumerable.Range(0, nrow).Sum(x => info.GetRowHeight(x, mt));
                ewidth = Enumerable.Range(0, ncol).Sum(x => info.GetColumnmWidth(x, mt));
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
