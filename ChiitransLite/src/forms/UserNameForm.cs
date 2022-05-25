using System.Windows.Forms;

namespace ChiitransLite.forms
{
public partial class UserNameForm: Form
{
	public UserNameForm() => InitializeComponent();

	internal bool Open(string key, string sense, string nameType)
	{
		textBoxKey.Text = key;
		textBoxSense.Text = sense;

		switch(nameType)
		{
		case "masc":
			radioMale.Checked = true;
			break;
		case "fem":
			radioFemale.Checked = true;
			break;
		case "surname":
			radioSurname.Checked = true;
			break;
		case "notname":
			radioNotName.Checked = true;
			break;
		default:
			radioOther.Checked = true;
			break;
		}

		return ShowDialog() == DialogResult.OK;
	}

	internal string getKey() => textBoxKey.Text;

	internal string getSense() => textBoxSense.Text;

	internal string getNameType()
	{
		if(radioMale.Checked)
			return "masc";
		else if(radioFemale.Checked)
			return "fem";
		else if(radioSurname.Checked)
			return "surname";
		else if(radioNotName.Checked)
			return "notname";
		else
			return null;
	}
}
}
