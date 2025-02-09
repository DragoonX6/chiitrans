using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

using ChiitransLite.misc;
using ChiitransLite.settings;

namespace ChiitransLite.forms
{
public partial class OptionsForm: Form
{
	private static OptionsForm _instance = null;

	public static OptionsForm instance
	{
		get
		{
			if(_instance == null)
				_instance = new OptionsForm();

			return _instance;
		}
	}

	private class InteropMethods
	{
		private readonly OptionsForm form;

		public InteropMethods(OptionsForm form) => this.form = form;

		public object getOptions() => form.getOptions();

		public void saveOptions(JsonElement op) => form.saveOptions(op);

		public void close() => form.Close();

		public void showPoFiles()
		{
			form.Invoke(new Action(() =>
			{
				new POFilesForm().ShowDialog();
			}));
		}

		public void showExtraTranslators()
		{
			form.Invoke(new Action(() =>
			{
				new ExtraTranslatorsForm().ShowDialog();
			}));
		}

		public void showHookForm() => form.showHookForm();

		public void resetParsePreferences() => form.resetParsePreferences();

		public void showNamesForm() => form.showNamesForm();
	}

	public OptionsForm()
	{
		InitializeComponent();
		FormUtil.restoreLocation(this);

		webBrowser1.ObjectForScripting =
			new BrowserInterop(webBrowser1, new InteropMethods(this));

		webBrowser1.Url = Utils.getUriForBrowser("options.html");
	}

	private void HookOptionsForm_Move(object sender, EventArgs e) =>
		FormUtil.saveLocation(this);

	private void HookOptionsForm_Resize(object sender, EventArgs e) =>
		FormUtil.saveLocation(this);

	private void HookOptionsForm_FormClosing(object sender, FormClosingEventArgs e)
	{
		if(e.CloseReason == CloseReason.UserClosing)
		{
			e.Cancel = true;
			Hide();
			TranslationForm.instance.moveBackgroundForm();
		}
	}

	private object getOptions()
	{
		bool isDefaultSession = Settings.session.processExe == null;

		return new
		{
			clipboard           = Settings.app.clipboardTranslation,
			sentenceDelay       = Settings.session.sentenceDelay.TotalMilliseconds,
			enableHooks         = !isDefaultSession,
			enableSentenceDelay = !isDefaultSession,
			display             = Settings.app.translationDisplay.ToString(),
			okuri               = Settings.app.okuriganaType.ToString(),
			theme               = Settings.app.cssTheme,
			themes              = getThemes(),
			separateWords       = Settings.app.separateWords,
			separateSpeaker     = Settings.app.separateSpeaker,
			nameDict            = Settings.app.nameDict.ToString(),
			stayOnTop           = Settings.app.stayOnTop,
			clipboardJapanese   = Settings.app.clipboardJapanese
		};
	}

	internal void saveOptions(JsonElement op)
	{
		bool clipboard         = op.GetProperty("clipboard").GetBoolean();
		int sentenceDelay      = op.GetProperty("sentenceDelay").GetInt32();
		string displayStr      = op.GetProperty("display").GetString();
		string okuriStr        = op.GetProperty("okuri").GetString();
		string theme           = op.GetProperty("theme").GetString();
		bool separateWords     = op.GetProperty("separateWords").GetBoolean();
		bool separateSpeaker   = op.GetProperty("separateSpeaker").GetBoolean();
		string nameDictStr     = op.GetProperty("nameDict").GetString();
		bool stayOnTop         = op.GetProperty("stayOnTop").GetBoolean();
		bool clipboardJapanese = op.GetProperty("clipboardJapanese").GetBoolean();

		TranslationForm.instance.setClipboardTranslation(clipboard);

		if(sentenceDelay >= 10)
			Settings.session.sentenceDelay =
				TimeSpan.FromMilliseconds(sentenceDelay);

		TranslationDisplay display;
		OkuriganaType okuri;
		NameDictLoading nameDict;

		if(Enum.TryParse(displayStr, out display))
			Settings.app.translationDisplay = display;

		if(Enum.TryParse(okuriStr, out okuri))
			Settings.app.okuriganaType = okuri;

		if(Enum.TryParse(nameDictStr, out nameDict))
			Settings.app.nameDict = nameDict;

		Settings.app.cssTheme          = theme;
		Settings.app.separateWords     = separateWords;
		Settings.app.separateSpeaker   = separateSpeaker;
		Settings.app.stayOnTop         = stayOnTop;
		Settings.app.clipboardJapanese = clipboardJapanese;

		TranslationForm.instance.applyCurrentSettings();
	}

	public void updateAndShow()
	{
		if(!Visible)
			webBrowser1.callScript("resetOptions", Utils.toJson(getOptions()));

		Show();
		TranslationForm.instance.moveBackgroundForm();

		Task.Factory.StartNew(() =>
		{
			// some fcking bug, cannot make the form visible on top without deferring activation
			Invoke(new Action(() => Activate()));
		});
	}


	internal void showHookForm()
	{
		UserHookForm.instance.Show();

		if(UserHookForm.instance.WindowState == FormWindowState.Minimized)
			UserHookForm.instance.WindowState = FormWindowState.Normal;

		UserHookForm.instance.Activate();
	}

	internal void showNamesForm()
	{
		Invoke(new Action(() =>
		{
			var namesForms = Application.OpenForms.OfType<NamesForm>().ToList();

			if(namesForms.Count > 0)
				namesForms[0].Activate();
			else
				new NamesForm().Show();
		}));
	}

	private IEnumerable<string> getThemes() =>
		Directory.GetFiles(
			Path.Combine(Utils.getRootPath(), "www\\themes"),
			"*.css")
		.Select(Path.GetFileNameWithoutExtension);

	internal void resetParsePreferences()
	{
		if(Utils.confirm(
				"Reset all parse preferences?\r\n"
				 + "This includes selected dictionary pages, "
				 + "user names and parse result bans."))
		{
			Settings.app.resetSelectedPages();
			Settings.app.resetWordBans();
			Settings.app.resetSelectedReadings();
			Settings.session.resetUserNames();

			Utils.info("Parse preferences have been reset to default.");
		}
	}
}
}
