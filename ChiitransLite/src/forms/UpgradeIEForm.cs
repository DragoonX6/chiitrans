using System;
using System.Diagnostics;
using System.Windows.Forms;

using ChiitransLite.settings;

namespace ChiitransLite.forms
{
public partial class UpgradeIEForm: Form
{
	public UpgradeIEForm() => InitializeComponent();

	private void checkBox1_CheckedChanged(object sender, EventArgs e) =>
		Settings.app.ieUpgradeAsk = !checkBox1.Checked;

	private void linkLabel1_LinkClicked(
		object sender,
		LinkLabelLinkClickedEventArgs e)
	{
		Process.Start(
			"http://www.microsoft.com/en-us/download/"
			+ "internet-explorer-10-details.aspx");
	}
}
}
