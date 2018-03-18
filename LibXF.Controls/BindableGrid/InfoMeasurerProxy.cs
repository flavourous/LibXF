using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LibXF.Controls.BindableGrid
{
    class InfoMeasurerProxy : ICellInfoManager
    {
        readonly Func<int, double> MeasureRow, MeasureColumn;
        public InfoMeasurerProxy(Func<int, double> MeasureRow, Func<int, double> MeasureColumn)
        {
            this.MeasureRow = MeasureRow;
            this.MeasureColumn = MeasureColumn;
        }

        // Calculate any "measure me" rows or columns
        ICellInfoManager info;
        public void SetInfo(ICellInfoManager info)
        {
            this.info = info;
            MeasureOverrideColumns.Clear();
            MeasureOverrideRows.Clear();
        }

        readonly Dictionary<int, double> MeasureOverrideColumns = new Dictionary<int, double>();
        readonly Dictionary<int, double> MeasureOverrideRows = new Dictionary<int, double>();

        public double GetColumnmWidth(int col, IList<IList> src)
        {
            var bm = info.GetColumnmWidth(col, src);
            if (bm == -1) // measure
            {
                if (MeasureOverrideColumns.ContainsKey(col))
                    return MeasureOverrideColumns[col];
                else return MeasureOverrideColumns[col] = MeasureColumn(col);
            }
            else return bm;
        }

        public int GetColumnSpan(object cellData)
        {
            return info.GetColumnSpan(cellData);
        }

        public double GetRowHeight(int row, IList<IList> src)
        {
            var bm = info.GetRowHeight(row, src);
            if (bm == -1) // measure
            {
                if (MeasureOverrideRows.ContainsKey(row))
                    return MeasureOverrideRows[row];
                else return MeasureOverrideRows[row] = MeasureRow(row);
            }
            else return bm;
        }

        public int GetRowSpan(object cellData)
        {
            return info.GetRowSpan(cellData);
        }
    }
}
