using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VirtualizingUtils;
using Xamarin.Forms;

namespace LibXF.Controls.BindableGrid
{
    static class ext
    {
        public static void Exhaust<T>(this Queue<T> q, Action<T> a)
        {
            while (q.Count > 0)
                a(q.Dequeue());
        }
    }
    class VirtualizedGridLayout : Layout<View>
    {
        class VTools: IntersectionFinder.FindIntersectionTools<int>
        {
            class dd : IDisposable { public void Dispose() => throw new NotImplementedException(); }
            double extent;
            public VTools(Func<int,double> Measure)
            {
                generate_next = Next;
                reset_generator = Reset;
                get_desired_height = Measure;
                temporarily_realize_item = i => delegate { };
            }

            public void SetExtent(int total, double extent)
            {
                this.total_items = total;
                this.extent = total;
            }

            int cfIndex;
            double cfIntersection;
            double cfScroll;
            const double buffer = 300.0;
            public int Update(double nScroll)
            {
                var offset = Math.Max(0, cfScroll - buffer);
                var target = nScroll - buffer;
                var intersection = cfIntersection;
                var start = cfIndex;
                var scanResult = Scan(cfIndex)
            }

            (int index, double offset, double intersection) Scan(int start, double intersection, double offset, double target)
            {
                bool forward = target >= offset; // intersection included within offset.

                // Progressive scan not needed
                if (target <= 0 && !forward) return (0, 0, 0);
                if (target >= extent && forward) return (total_items - 1, extent, get_desired_height(total_items - 1));

                // setup VVirtual
                forwardDirection = forward;
                this.start = start;
                this.current = start - 1;
                var reached = start;

                // call VVirtual
                var res = IntersectionFinder.FindIntersection<int>(new IntersectionFinder.FindIntersecionAndOffsetArgs
                {
                    initialIntersection = intersection,
                    initialValue = offset,
                    targetValue = target
                }, this, ref reached);

                return (reached, res.valueReached, res.intersection);
            }

            int current, start;
            int Next() => ++current;
            IDisposable Reset() { current = start -1; return new dd(); }
        }

        readonly ICellInfoManager info;
        readonly IList<IList> cells;
        readonly DataTemplate template;
        readonly VTools rScanner, cScanner;
        public VirtualizedGridLayout(ICellInfoManager info, IList<IList> cells, DataTemplate template)
        {
            this.info = info;
            this.cells = cells;
            this.template = template;
            rScanner = new VTools(info.GetRowHeight);
            cScanner = new VTools(info.GetColumnmWidth);
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();
            if(Parent is ScrollView psv)
            {
                if (lp != null) lp.PropertyChanged -= Psv_PropertyChanged;
                psv.PropertyChanged += Psv_PropertyChanged;
                lp = psv;
            }
        }

        private ScrollView lp;
        private void Psv_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == ScrollView.ScrollXProperty.PropertyName 
                || e.PropertyName == ScrollView.ScrollYProperty.PropertyName)
                InvalidateViewportCells();
            if (e.PropertyName == View.WidthProperty.PropertyName
                 || e.PropertyName == View.HeightProperty.PropertyName)
                InvalidateExtent();
        }

        // recalculate them if required, intent to give LayoutChildren enough information to reogranize and recycle
        double lSX, lSY;
        private void InvalidateViewportCells()
        {
            var sx = lp?.ScrollX ?? 0.0;
            var sy = lp?.ScrollY ?? 0.0;
            if (lSX != sx || lSY != sy)
            {
                // cache buster
                runLayout = true;
                lSX = sx;
                lSY = sy;
                InvalidateLayout();
            }
        }

        // buffer
        const double buffer = 300.0; // constant buffer

        // first item layed out currently
        int cfRow, cfCol; // the item idex
        double cfIRow, cfICol; // last intersection
        double cfSX, cfSY; // last used scroll value

        readonly Dictionary<(int r, int c), View> CellViewIndex = new Dictionary<(int r, int c), View>();
        
        bool runLayout = false;
        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            // blocks recursion and layouts we may not need to perform
            if (!runLayout) return;
            runLayout = false;

            // setup scanners
            rScanner.SetExtent(cells.Count, eheight);
            cScanner.SetExtent(cells.Count == 0 ? 0 : cells.Max(c => c.Count), ewidth);

            // For the rows - thinking through it

            var srow = rScanner.Scan(cfRow,, Math.Max(0, cfSY - buffer), lSY - buffer);
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
                ncol = cells.Max(x => x.Count);
                eheight = Enumerable.Range(0, nrow).Sum(x => info.GetRowHeight(x));
                ewidth = Enumerable.Range(0, ncol).Sum(x => info.GetColumnmWidth(x));
                update = false;
            }
        }

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            CalculateExtent();
            return new SizeRequest(new Size(ewidth, eheight));
        }
    }
}
