using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MtrDev.WebView2.Interop
{
    /// <summary>
    /// Event args for the MoveFocusRequested event.
    /// </summary>
    [ComVisible(true)]
    [ComImport]
    [Guid("CE31A597-E202-49B9-A9BE-825481ED517E")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ICoreWebView2MoveFocusRequestedEventArgs
	{
        /// <summary>
        /// The reason for WebView to fire the MoveFocus Requested event.
        /// </summary>
		[DispId(1610678272)]
		WEBVIEW2_MOVE_FOCUS_REASON Reason
		{			
			get;
		}

        /// <summary>
        /// Indicate whether the event has been handled by the app.
        /// If the app has moved the focus to its desired location, it should set
        /// Handled property to TRUE.
        /// When Handled property is false after the event handler returns, default
        /// action will be taken. The default action is to try to find the next tab
        /// stop child window in the app and try to move focus to that window. If
        /// there is no other such window to move focus to, focus will be cycled
        /// within the WebView's web content.
        /// </summary>
        [DispId(1610678273)]
        bool Handled
        {
            [return: MarshalAs(UnmanagedType.Bool)]
            get;
            [param: MarshalAs(UnmanagedType.Bool)]
            set;
        }

    }
}