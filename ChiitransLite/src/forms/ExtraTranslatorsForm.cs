using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using ChiitransLite.settings;

namespace ChiitransLite.forms
{
public partial class ExtraTranslatorsForm: Form
{
	public ExtraTranslatorsForm()
	{
		InitializeComponent();
	}

	private ListBox other(ListBox listBox)
	{
		if(listBox == listBox1)
			return listBox2;
		else
			return listBox1;
	}

	private void listBox1_MouseMove(object sender, MouseEventArgs e)
	{
		if((e.Button & System.Windows.Forms.MouseButtons.Left) != 0)
		{
			ListBox listBox = sender as ListBox;

			if(listBox.SelectedItem != null)
				listBox.DoDragDrop(listBox.SelectedItem, DragDropEffects.Move);
		}
	}

	private void listBox1_DragOver(object sender, DragEventArgs e) =>
		e.Effect = DragDropEffects.Move;

	private void listBox1_DragDrop(object sender, DragEventArgs e)
	{
		ListBox listBox = sender as ListBox;
		Point point = listBox.PointToClient(new(e.X, e.Y));
		int i = listBox.IndexFromPoint(point);

		object oldItem = null;

		if(i != ListBox.NoMatches)
			oldItem = listBox.Items[i];

		object data = e.Data.GetData(typeof(string));

		if(data != null && oldItem != data)
		{
			listBox1.Items.Remove(data);
			listBox2.Items.Remove(data);

			if(oldItem != null)
				listBox.Items.Insert(i, data);
			else
				listBox.Items.Add(data);

			listBox.SelectedItem = data;
		}
	}

	private void listBox1_MouseDown(object sender, MouseEventArgs e)
	{
		var listBox = sender as ListBox;
		other(listBox).ClearSelected();

		if(e.Clicks > 1 && (e.Button & MouseButtons.Left) != 0)
		{
			int i = listBox.IndexFromPoint(e.Location);

			if(i != ListBox.NoMatches)
			{
				object it = listBox.Items[i];

				listBox.Items.RemoveAt(i);

				other(listBox).Items.Add(it);
				other(listBox).SelectedItem = it;
			}
		}
	}

	private void ExtraTranslatorsForm_Load(object sender, EventArgs e)
	{
		List<string> selectedTranslators = Settings.app.getSelectedTranslators();

		listBox1.Items.Clear();
		listBox1.Items.AddRange(selectedTranslators.ToArray());

		listBox2.Items.Clear();
		listBox2.Items.AddRange(
			Settings.app.getTranslators()
			.Except(selectedTranslators)
			.OrderBy(x => x.ToLower())
			.ToArray());
	}

	private void buttonOk_Click(object sender, EventArgs e) =>
		Settings.app.setSelectedTranslators(
			listBox1.Items.Cast<string>().ToList());
}
}
