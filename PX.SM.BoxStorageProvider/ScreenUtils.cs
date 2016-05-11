using Box.V2.Exceptions;
using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

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

        //From PX.Api.OData.Model.GIDataService
        public static object UnwrapValue(object value)
        {
            PXFieldState state = value as PXFieldState;
            if (state != null)
            {
                if (state.Value == null && state.DefaultValue != null) // for a case when field has PXDefault attribute, but null value because of inconsistency in DB
                    state.Value = state.DefaultValue;
                if (state.Value != null && !state.DataType.IsInstanceOfType(state.Value)) // for a case when, for example, some field of int type has PXSelectorAttribute with substitute field of string type, and PXSelectorAttribute can't find corresponding row for substitute field
                    state.Value = Convert.ChangeType(state.Value, state.DataType);
            }

            PXStringState strState = value as PXStringState;
            if (strState != null && strState.Value != null && strState.ValueLabelDic != null && strState.ValueLabelDic.ContainsKey((string)strState.Value))
                return strState.ValueLabelDic[(string)strState.Value];
            PXIntState intState = value as PXIntState;
            if (intState != null && intState.Value != null && intState.ValueLabelDic != null)
                return intState.ValueLabelDic.ContainsKey((int)intState.Value) ? intState.ValueLabelDic[(int)intState.Value] : intState.Value.ToString();
            return PXFieldState.UnwrapValue(value);
        }

        public static void HandleAggregateException(AggregateException aggregateException, HttpStatusCode codeToHandle, Action<Exception> action)
        {
            aggregateException.Handle((e) =>
            {
                PXTrace.WriteError(e);
                var boxException = e as BoxException;
                if (boxException != null && boxException.StatusCode == codeToHandle)
                {
                    action(e);
                    return true;
                }

                return false;
            });
        }

        public static void TraceAndThrowException(string message, params object[] args)
        {
            var exception = new PXException(message, args);
            PXTrace.WriteError(exception);
            throw exception;
        }

        public static bool IsMatchingActivitiesFolderRegex(string text)
        {
            var regex = new Regex($@"{PXLocalizer.Localize(Messages.ActivitiesFolderName)}\\");
            return regex.IsMatch(text);
        }
    }
}
