using System;
using System.Windows.Forms;

namespace ChiitransLite.misc
{
static class WebBrowserExt
{
	public static object callScript(this WebBrowser browser, String fnName, params object[] args)
	{
		if(!browser.IsDisposed)
		{
			if(browser.InvokeRequired)
				return browser.Invoke(new Func<object>(() => browser.Document.InvokeScript(fnName, args)));
			else
				return browser.Document.InvokeScript(fnName, args);
		}
		else
			return null;
	}
}
}
