using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using System.Web.Compilation;
using PX.Data.Description;

namespace PX.SM.BoxStorageProvider
{
    public static class ScreenUtils
    {
        //Copied from PX.Data.Handlers.PXEntityOpener (it is private)
        public static void SelectCurrent(PXView primaryView, PX.Data.Description.PXViewDescription primaryViewInfo, params KeyValuePair<string, string>[] pars)
        {
            //selecting data
            List<object> searches = new List<object>();
            List<object> parameters = new List<object>();
            List<string> sortCols = new List<string>();
            List<bool> descendings = new List<bool>();
            int startRow = 0, totalRows = 0;
            foreach (KeyValuePair<string, string> pair in pars)
            {
                if (!primaryView.Cache.Keys.Contains(pair.Key)) return;

                PXFieldState state = primaryView.Cache.GetStateExt(null, pair.Key) as PXFieldState;
                object val = Convert.ChangeType(pair.Value, state.DataType);
                searches.Add(val);
                sortCols.Add(pair.Key);
                descendings.Add(false);

                if (primaryViewInfo.Parameters.Any(p => p.Name == pair.Key)) parameters.Add(val);
            }
            List<object> recs = primaryView.Select(null, parameters.ToArray(), searches.ToArray(), sortCols.ToArray(), descendings.ToArray(), null, ref startRow, 1, ref totalRows);
            if (recs == null || recs.Count == 0) return;

            Object current = recs[0] is PXResult ? ((PXResult)recs[0])[primaryView.Cache.GetItemType()] : recs[0];
            if (current == null) return;

            primaryView.Cache.Current = current;
        }
    }
}
