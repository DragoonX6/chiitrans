using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using ChiitransLite.texthook.ext;

namespace ChiitransLite.forms
{
public partial class ContextLogForm: Form
{
	private static Dictionary<int, ContextLogForm> forms = new();

	private MyContext ctx;

	public ContextLogForm() => InitializeComponent();

	internal void setContext(MyContext ctx)
	{
		this.ctx = ctx;

		Text = ctx.getFullName() + " - Log";
		textBox1.AppendText(string.Join("\r\n\r\n", ctx.getLog()));

		ctx.onSentence += onContextSentence;
	}

	private void onContextSentence(texthook.TextHookContext sender, string text)
	{
		Invoke(new Action(() =>
		{
			if(textBox1.TextLength > 10000)
			{
				textBox1.Text = "";
				textBox1.AppendText(string.Join("\r\n\r\n", ctx.getLog()));
			}
			else
				textBox1.AppendText("\r\n\r\n" + text);
		}));
	}

	private Point getLocationNear(Form parentForm)
	{
		Rectangle bounds = Screen.GetWorkingArea(parentForm);
		Point target = Point.Add(parentForm.Location, new(parentForm.Width, 0));

		if(!bounds.Contains(Point.Add(target, Size)))
			target = Point.Add(parentForm.Location, new(50, 50));

		return target;
	}

	internal static ContextLogForm getForContext(MyContext ctx, Form parentForm)
	{
		ContextLogForm res;

		if(!forms.TryGetValue(ctx.id, out res))
		{
			res = new();
			res.Location = res.getLocationNear(parentForm);
			res.setContext(ctx);

			forms.Add(ctx.id, res);
		}

		return res;
	}

	private void ContextLogForm_FormClosed(object sender, FormClosedEventArgs e)
	{
		if(ctx != null)
		{
			ctx.onSentence -= onContextSentence;
			forms.Remove(ctx.id);
		}
	}
}
}
