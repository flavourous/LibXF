using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LibXF.Controls
{
    internal static class Utils
    {
        public static List<List<object>> Expand(this IEnumerable dual)
        {
            return Expand(dual, x => x);
        }
        public static List<List<T>> Expand<T>(this IEnumerable dual, Func<object,T> selector)
        {
            var ret = new List<List<T>>();
            if (dual != null)
            {
                foreach (var r in dual)
                {
                    var rd = new List<T>();
                    foreach (var c in r as IEnumerable)
                        rd.Add(selector(c));
                    ret.Add(rd);
                }
            }
            return ret;
        }
    }
}
