using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

using ChiitransLite.misc;
using ChiitransLite.texthook;
using ChiitransLite.texthook.ext;
using ChiitransLite.translation;
using ChiitransLite.settings;

namespace ChiitransLite.forms
{
public partial class MainForm: Form
{
	public class InteropMethods
	{
		private MainForm form;

		internal InteropMethods(MainForm form) => this.form = form;

		public object getProcesses()
		{
			int curPid = Process.GetCurrentProcess().Id;
			var lastRun = Properties.Settings.Default.lastRun;
			var pid = 0;

			var res = Process.GetProcesses().SelectMany((p) =>
			{
				String name;

				try
				{
					name = p.MainModule.FileName;
				}
				catch
				{
					return Enumerable.Empty<object>();
				}

				if(p.Id == curPid || string.IsNullOrEmpty(p.MainWindowTitle))
					return Enumerable.Empty<object>();
				else
				{
					if(name == lastRun)
						pid = p.Id;

					return Enumerable.Repeat(new
					{
						pid = p.Id,
						name = name
					}, 1);
				}
			}).ToList();

			return new
			{
				procs = res,
				defaultPid = pid,
				defaultName = lastRun
			};
		}

		public void selectWindowClick(JsonElement isSelectWindow) =>
			form.startSelectWindow(isSelectWindow.GetBoolean());

		public void browseClick() => form.showBrowseDialog();

		public void connectClick(JsonElement pid, JsonElement exeName)
		{
			try
			{
				TextHook.instance.connect(pid.GetInt32(), exeName.GetString());
				form.connectSuccess();
			}
			catch(Exception ex)
			{
				form.connectError(ex.Message);
			}
		}

		public void setContextEnabled(JsonElement ctxId, JsonElement isEnabled)
		{
			var ctx = (MyContext)TextHook.instance.getContext(ctxId.GetInt32());

			if(ctx != null)
				ctx.enabled = isEnabled.GetBoolean();
		}

		public void setContextEnabledOnly(JsonElement ctxId)
		{
			foreach(MyContext ctx in TextHook.instance.getContexts().Cast<MyContext>())
			{
				if(ctx.id == ctxId.GetInt32())
					ctx.enabled = true;
				else
					ctx.enabled = false;
			}
		}

		public void showTranslationForm()
		{
			form.showTranslationForm();
		}

		public void setNewContextsBehavior(JsonElement b) =>
			(TextHook.instance.getContextFactory() as MyContextFactory)
				.setNewContextsBehavior(b.GetString());

		public void showLog(JsonElement ctxId) => form.showLog(ctxId.GetInt32());

		public void translate(JsonElement text) => form.translate(text.GetString());

		public object getContext(JsonElement ctxId)
		{
			var ctx = (MyContext)TextHook.instance.getContext(ctxId.GetInt32());

			if(ctx != null)
			{
				return new
				{
					id = ctx.id,
					name = ctx.name,
					addr = ctx.context,
					sub = ctx.subcontext,
					enabled = ctx.enabled
				};
			}
			else
				return null;
		}

		public void showAbout()
		{
			form.Invoke(new Action(() =>
			{
				TranslationForm.instance.SuspendTopMost(() =>
				{
					new AboutForm().ShowDialog();
				});
			}));
		}

		public void showOptions() => form.showOptions();
	}

	private static MainForm _instance;

	public static MainForm instance
	{
		get => _instance;
	}

	public MainForm()
	{
		_instance = this;

		InitializeComponent();
		FormUtil.restoreLocation(this);

		webBrowser1.ObjectForScripting =
			new BrowserInterop(webBrowser1, new InteropMethods(this));

		webBrowser1.Url = Utils.getUriForBrowser("index.html");

		TextHook.instance.setContextFactory(
			new MyContextFactory(TextHook.instance));

		/* Logger.onLog += (text) => {
			webBrowser1.callScript("log", "DEBUG: " + text);
		}; */
	}

	private bool isSelectWindow = false;

	private void startSelectWindow(bool isSelectWindow) =>
		this.isSelectWindow = isSelectWindow;

	private void MainForm_Deactivate(object sender, EventArgs e)
	{
		if(isSelectWindow)
		{
			isSelectWindow = false;
			webBrowser1.callScript("selectWindowEnd");

			Task.Factory.StartNew(() =>
			{
				IntPtr newWindow = Winapi.GetForegroundWindow();

				if(newWindow != IntPtr.Zero)
				{
					uint pid;
					Winapi.GetWindowThreadProcessId(newWindow, out pid);
					setDefaultProcess((int)pid);
				}
				else
				{
					Thread.Sleep(100);
					newWindow = Winapi.GetForegroundWindow();

					if(newWindow != IntPtr.Zero)
					{
						uint pid;
						Winapi.GetWindowThreadProcessId(newWindow, out pid);
						setDefaultProcess((int)pid);
					}
					else
					{
						Thread.Sleep(500);
						newWindow = Winapi.GetForegroundWindow();

						if(newWindow != IntPtr.Zero)
						{
							uint pid;
							Winapi.GetWindowThreadProcessId(newWindow, out pid);
							setDefaultProcess((int)pid);
						}
					}
				}
			});
		}
	}

	private void setDefaultProcess(int pid, string name = null)
	{
		try
		{
			if(name == null)
				name = Process.GetProcessById(pid).MainModule.FileName;

			webBrowser1.callScript("setDefaultProcess", pid, name);
		}
		catch
		{
		}
	}

	private void showBrowseDialog()
	{
		TranslationForm.instance.SuspendTopMost(() =>
		{
			if(openExeFile.ShowDialog() == DialogResult.OK)
				setDefaultProcess(0, openExeFile.FileName);
		});
	}

	private void connectError(string errMsg) =>
		webBrowser1.callScript("connectError", errMsg);

	private bool eventsSetUp = false;

	private void connectSuccess()
	{
		MyContextFactory fac = TextHook.instance.getContextFactory() as MyContextFactory;
		webBrowser1.callScript("connectSuccess", fac.getNewContextsBehaviorAsString());

		if(!eventsSetUp)
		{
			eventsSetUp = true;

			TextHook.instance.onNewContext += (ctx) =>
			{
				webBrowser1.callScript("newContext", ctx.id, ctx.name, ctx.context, ctx.subcontext, (ctx as MyContext).enabled);
				ctx.onSentence += ctx_onSentence;
				List<int> disabledContexts = fac.disableContextsIfNeeded(ctx);

				if(disabledContexts != null && disabledContexts.Count > 0)
				{
					webBrowser1.callScript("disableContexts", Utils.toJson(disabledContexts));
				}
			};

			TextHook.instance.onDisconnect += () =>
			{
				webBrowser1.callScript("disconnect");
				TranslationForm.instance.Close();
			};
		}

		Invoke(new Action(() =>
		{
			TranslationForm.instance.setCaption(TextHook.instance.currentProcessTitle + " - Chiitrans Lite");
			showTranslationForm();
		}));
	}

	private void ctx_onSentence(TextHookContext sender, string text)
	{
		var ctx = (MyContext)sender;

		if(ctx.enabled)
			TranslationService.instance.update(text);

		webBrowser1.callScript("newSentence", sender.id, text);
	}

	private void showTranslationForm()
	{
		// a nasty bug with disappearing form workaround
		if(TranslationForm.instance.Visible && Settings.app.transparentMode && TranslationForm.instance.WindowState != FormWindowState.Minimized)
		{
			TranslationForm.instance.TransparencyKey = Color.Empty;
			TranslationForm.instance.setTransparentMode(true, false);
		}

		TranslationForm.instance.Show();

		if(TranslationForm.instance.WindowState == FormWindowState.Minimized)
			TranslationForm.instance.WindowState = FormWindowState.Normal;

		TranslationForm.instance.Activate();
	}

	private void MainForm_Move(object sender, EventArgs e) =>
		FormUtil.saveLocation(this);

	private void MainForm_Resize(object sender, EventArgs e) =>
		FormUtil.saveLocation(this);

	internal void showLog(int ctxId)
	{
		var ctx = (MyContext)TextHook.instance.getContext(ctxId);

		if(ctx != null)
		{
			ContextLogForm form = ContextLogForm.getForContext(ctx, this);
			form.Show();
			form.Activate();
		}
	}

	private void MainForm_Shown(object sender, EventArgs e)
	{
		var ieVer = webBrowser1.Version.Major;
		string mm = webBrowser1.Version.Major + "." + webBrowser1.Version.Minor;

		if(ieVer < 8)
		{
			MessageBox.Show(
				"You are using an outdated version of Internet Explorer: "
				+ mm + "\r\n\r\n"
				+ "Chiitrans Lite requires Internet Explorer 8 or later to be "
				+ "installed. Please upgrade.",
				"Warning",
				MessageBoxButtons.OK,
				MessageBoxIcon.Warning);
		}
		else
		{
			if(ieVer == 8
			   && Utils.isWindowsVistaOrLater()
			   && Settings.app.ieUpgradeAsk)
			{
				new UpgradeIEForm().ShowDialog();
			}
		}
	}

	internal void showOptions() => OptionsForm.instance.updateAndShow();

	internal void translate(string text) =>
		TranslationService.instance.update(
			TextHookContext.cleanText(text),
			false);
}
}
