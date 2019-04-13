///////////////////////////////////////////////////////////////////////////////
// CapHelper
//
// This software is released into the public domain.  You are free to use it
// in any way you like, except that you may not sell this source code.
//
// This software is provided "as is" with no expressed or implied warranty.
// I accept no liability for any damage or loss of business that this software
// may cause.
// 
// This source code is originally written by Tamir Khason (see http://blogs.microsoft.co.il/blogs/tamir
// or http://www.codeplex.com/wpfcap).
// 
// Modifications are made by Geert van Horrik (CatenaLogic, see http://blog.catenalogic.com) 
// 
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace WpfCap
{
    /// <summary>
    /// Class that helps finding the pins for a specific filter
    /// </summary>
    internal static class CapHelper
    {
		/// <summary>
		/// From AMPROPERTY_PIN
		/// </summary>
		private enum eAMPropertyPin
		{
			Category = 0,
			Medium = 0
		}

		/// <summary>
		/// Returns the PinCategory of the specified pin.  Usually a member of PinCategory.  Not all pins have a category.
		/// </summary>
		/// <param name="pin"></param>
		/// <returns>GUID indicating pin category or Guid.Empty on no category.  Usually a member of PinCategory</returns>
		public static Guid GetPinCategory(this IPin pin)
		{
			Guid categoryGuid = Guid.Empty;

			// Memory to hold the returned GUID
			int sizeOfGuid = Marshal.SizeOf(typeof(Guid));
			IntPtr ipOut = Marshal.AllocCoTaskMem(sizeOfGuid);

			try
			{
				int cbBytes;
				var g = CapDevice.Pin;

				// Get an IKsPropertySet from the pin
				var propertySet = pin as IKsPropertySet;

				if (propertySet != null)
				{
					// Query for the Category
					var hr = propertySet.Get(g, (int)eAMPropertyPin.Category, IntPtr.Zero, 0, ipOut, sizeOfGuid, out cbBytes);

					// Marshal it to the return variable
					categoryGuid = (Guid)Marshal.PtrToStructure(ipOut, typeof(Guid));
				}
			}
			finally
			{
				Marshal.FreeCoTaskMem(ipOut);
				ipOut = IntPtr.Zero;
			}

			return categoryGuid;
		}

		public static IPin GetPin(this IBaseFilter filter, Guid category, int index)
		{
			return filter.EnumeratePinsUntil((pin, i) =>
				{
					bool found = false;
					var pinCategory = pin.GetPinCategory();
					if (pinCategory == category)
					{ found = (i == index); }
					return found;
				});
		}

        /// <summary>
        /// Gets a specific pin of a specific filter
        /// </summary>
        /// <param name="filter">Filter to retrieve the pin for (defines which object should make this method available)</param>
        /// <param name="dir">Direction</param>
        /// <param name="num">Number</param>
        /// <returns>IPin object or null</returns>
        public static IPin GetPin(this IBaseFilter filter, PinDirection dir, int index)
        {
			return filter.EnumeratePinsUntil((pin, i) =>
			{
				bool found = false;

				// Query the direction
				PinDirection pinDir;
				pin.QueryDirection(out pinDir);

				// Is the pin direction correct?
				if (pinDir == dir)
				{ found = (i == index); }

				return found;
			});
        }

		private static IPin EnumeratePinsUntil(this IBaseFilter filter, Func<IPin, int, bool> predicate)
		{
			// Declare variables
			IPin foundPin = null;
			IPin[] pin = new IPin[1];
			IEnumPins pinsEnum = null;

			// Enumerator the pins
			if (filter.EnumPins(out pinsEnum) == 0)
			{
				int n = 0;

				// Loop the pins
				while (pinsEnum.Next(1, pin, out n) == 0)
				{
					if (predicate(pin[0], n-1))
					{
						foundPin = pin[0];
						break;
					}

					// Release the pin, this is not the one we are looking for
					Marshal.ReleaseComObject(pin[0]);
					pin[0] = null;
				}
			}

			// Not found
			return foundPin;
		}
    }
}
