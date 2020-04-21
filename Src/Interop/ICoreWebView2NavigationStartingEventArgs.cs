using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MtrDev.WebView2.Interop
{
    /// <summary>
    /// Event args for the NavigationStarting event.
    /// </summary>
    [ComVisible(true)]
    [ComImport]
    [Guid("1C81A448-575B-44A1-9ABD-1B93A3DE9E03")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ICoreWebView2NavigationStartingEventArgs
	{
        /// <summary>
        /// The uri of the requested navigation.
        /// </summary>
        [DispId(1610678272)]
        string Uri
        {
            [return: MarshalAs(UnmanagedType.LPWStr)]
            get;
        }

        /// <summary>
        /// True when the navigation was initiated through a user gesture as opposed
        /// to programmatic navigation.
        /// </summary>
        [DispId(1610678273)]
        bool IsUserInitiated
        {
            [return: MarshalAs(UnmanagedType.Bool)]
            get;
        }

        /// <summary>
        /// True when the navigation is redirected.
        /// </summary>
        [DispId(1610678274)]
        bool IsRedirected
        {
            [return: MarshalAs(UnmanagedType.Bool)]
            get;
        }

        /// <summary>
        /// The HTTP request headers for the navigation.
        /// </summary>
        [DispId(1610678275)]
        ICoreWebView2HttpRequestHeaders RequestHeaders
        {
            get;
        }

        /// <summary>
        /// The host may set this flag to cancel the navigation.
        /// If set, it will be as if the navigation never happened and the current
        /// page's content will be intact. For performance reasons, GET HTTP requests
        /// may happen, while the host is responding. This means cookies can be set
        /// and used part of a request for the navigation.
        /// </summary>
        [DispId(1610678276)]
		bool Cancel
		{
            [return: MarshalAs(UnmanagedType.Bool)]
            get;
            [param: MarshalAs(UnmanagedType.Bool)]
            set;
        }

        long NavigationId
        {
            get;
        }
    }
}