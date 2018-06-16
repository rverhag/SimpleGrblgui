using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace WpfCap
{
    public class FilterInfo : IComparable
    {
        public readonly string Name;
        public readonly string MonikerString;

        internal FilterInfo(IMoniker moniker)
        {
            moniker.GetDisplayName(null, null, out MonikerString);
            Name = GetName(moniker);
        }

        private string GetName(IMoniker moniker)
        {
            Object bagObj = null;
            IPropertyBag bag = null;

            try
            {
                Guid bagId = typeof(IPropertyBag).GUID;
                moniker.BindToStorage(null, null, ref bagId, out bagObj);
                bag = (IPropertyBag)bagObj;

                object val = "";
                int hr = bag.Read("FriendlyName", ref val, IntPtr.Zero);
                if (hr != 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                string ret = (string)val;
                if ((ret == null) || (ret.Length < 1))
                {
                    throw new ApplicationException();
                }

                return ret;
            }
            catch (Exception)
            {
                return "Unknown";
            }
            finally
            {
                bag = null;
                if (bagObj != null)
                {
                    Marshal.ReleaseComObject(bagObj);
                    bagObj = null;
                }
            }
        }

        public int CompareTo(object value)
        {
            FilterInfo f = (FilterInfo)value;
            return f == null ? 1 : (this.Name.CompareTo(f.Name));
        }

        internal static IBaseFilter CreateFilter(string filterMoniker)
        {
            object filterObject = null;
            IBindCtx bindCtx = null;
            IMoniker moniker = null;

            int n = 0;

            if (CreateBindCtx(0, out bindCtx) == 0)
            {
                if (MkParseDisplayName(bindCtx, filterMoniker, ref n, out moniker) == 0)
                {
                    Guid filterId = typeof(IBaseFilter).GUID;
                    moniker.BindToObject(null, null, ref filterId, out filterObject);

                    Marshal.ReleaseComObject(moniker);
                }
                Marshal.ReleaseComObject(bindCtx);
            }
            return filterObject as IBaseFilter;
        }

       


        //public FilterInfo(string monikerString)
        //{
        //    MonikerString = monikerString;
        //    Name = GetName(monikerString);
        //}

        //private string GetName(string monikerString)
        //{
        //    IBindCtx bindCtx = null;
        //    IMoniker moniker = null;
        //    String name = "";
        //    int n = 0;

        //    if (CreateBindCtx(0, out bindCtx) == 0)
        //    {
        //        if (MkParseDisplayName(bindCtx, monikerString, ref n, out moniker) == 0)
        //        {
        //            name = GetName(moniker);

        //            Marshal.ReleaseComObject(moniker);
        //            moniker = null;
        //        }
        //        Marshal.ReleaseComObject(bindCtx);
        //        bindCtx = null;
        //    }
        //    return name;
        //}

        [DllImport("ole32.dll")]
        public static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

        [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
        public static extern int MkParseDisplayName(IBindCtx pbc, string szUserName, ref int pchEaten, out IMoniker ppmk);
    }
}
