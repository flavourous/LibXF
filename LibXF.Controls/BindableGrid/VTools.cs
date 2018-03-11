using System;
using System.Collections.Generic;
using System.Text;
using VirtualizingUtils;

namespace LibXF.Controls.BindableGrid
{
    class ScanResult
    {
        public double placement;
        public int first, last;
    }
    class VTools : IntersectionFinder.FindIntersectionTools<int>
    {
        class dd : IDisposable { public void Dispose() { } }
        public VTools(Func<int, double> Measure)
        {
            generate_next = Next;
            reset_generator = Reset;
            get_desired_height = Measure;
            temporarily_realize_item = i => delegate { };
        }

        int cfIndex;
        double cfIntersection;
        double cfScroll;
        const double buffer = 100.0;
        public ScanResult Update(double scroll, double viewport, double extent, int total)
        {
            // setup
            total_items = total;
            var offset = Math.Max(0, cfScroll - buffer);
            var intersection = cfIntersection;
            var start = cfIndex;

            // scan for first item
            var startTarget = scroll + fakeOffset - buffer;
            var startResult = Scan(start, intersection, offset, startTarget, extent);

            // scan for last item
            var endTarget = scroll + viewport + fakeOffset + buffer;
            var endResult = Scan(start, intersection, offset, endTarget, extent);

            // store intermediates
            cfIndex = startResult.index;
            cfIntersection = startResult.intersection;
            cfScroll = scroll;

            // return range
            return new ScanResult { placement = startResult.offset - startResult.intersection - fakeOffset, first = startResult.index, last = endResult.index };
        }

        double fakeOffset = 0.0;
        public void SetOffset(double o) => fakeOffset = o;

        (int index, double offset, double intersection) Scan(int start, double intersection, double offset, double target, double extent)
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
        IDisposable Reset() { current = start - 1; return new dd(); }
    }
}
