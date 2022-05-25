using System;
using System.Windows.Forms;

using ChiitransLite.settings;

namespace ChiitransLite.forms
{
public partial class AtlasNotFoundForm: Form
{
	public AtlasNotFoundForm() => InitializeComponent();

	private void checkBox1_CheckedChanged(object sender, EventArgs e) =>
		Settings.app.atlasAsk = !checkBox1.Checked;
}
}
