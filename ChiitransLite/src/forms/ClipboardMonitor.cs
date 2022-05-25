using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using ChiitransLite.misc;

namespace ChiitransLite.forms
{
// Must inherit Control, not Component, in order to have Handle
[DefaultEvent("ClipboardChanged")]
public partial class ClipboardMonitor: Control
{
	IntPtr nextClipboardViewer;
	private bool initialized;

	public ClipboardMonitor()
	{
		BackColor = Color.Red;
		Visible = false;
	}

	public void initialize()
	{
		if(!initialized)
		{
			nextClipboardViewer = (IntPtr)SetClipboardViewer((int)this.Handle);
			initialized = true;
		}
	}

	/// <summary>
	/// Clipboard contents changed.
	/// </summary>
	public event EventHandler ClipboardChanged;

	[DllImport("User32.dll")]
	protected static extern int SetClipboardViewer(int hWndNewViewer);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	public static extern bool ChangeClipboardChain(
		IntPtr hWndRemove,
		IntPtr hWndNewNext);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	public static extern int SendMessage(
		IntPtr hwnd,
		int wMsg,
		IntPtr wParam, IntPtr lParam);

	protected override void WndProc(ref System.Windows.Forms.Message m)
	{
		// defined in winuser.h
		const int WM_DRAWCLIPBOARD = 0x308;
		const int WM_CHANGECBCHAIN = 0x030D;

		switch(m.Msg)
		{
		case WM_DRAWCLIPBOARD:
		{
			OnClipboardChanged();
			SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
		} break;
		case WM_CHANGECBCHAIN:
		{
			if(m.WParam == nextClipboardViewer)
				nextClipboardViewer = m.LParam;
			else
				SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
		} break;
		default:
		{
			base.WndProc(ref m);
		} break;
		}
	}

	void OnClipboardChanged()
	{
		try
		{
			if(ClipboardChanged != null)
				ClipboardChanged(this, null);

		}
		catch(Exception e)
		{
			// Swallow or pop-up, not sure
			// Trace.Write(e.ToString());
			Logger.logException(e);
		}
	}

	internal void Close()
	{
		try
		{
			if(initialized)
				ChangeClipboardChain(Handle, nextClipboardViewer);
		}
		catch
		{
		}
	}
}
}
